using FluentValidation;
using System.Text.Json.Nodes;

namespace DocflowRules.Api.Validation;

public class TestUpsertPayload
{
    public string Name { get; set; } = default!;
    public JsonObject Input { get; set; } = new();
    public JsonObject Expect { get; set; } = new();
    public string? Suite { get; set; }
    public string[]? Tags { get; set; }
    public int? Priority { get; set; }
}

public partial class TestUpsertValidator : AbstractValidator<TestUpsertPayload>
{
    public TestUpsertValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Il nome del test è obbligatorio").MaximumLength(200);
        RuleFor(x => x.Priority).InclusiveBetween(1,5).When(x=>x.Priority.HasValue).WithMessage("La priorità deve essere tra 1 e 5");
        RuleFor(x => x.Input).NotNull().WithMessage("L'input è obbligatorio");
        RuleFor(x => x.Expect).NotNull().WithMessage("L'expected è obbligatorio");
        RuleFor(x => x.Expect).Must(e => e["fields"] is JsonObject).When(x=>x.Expect!=null).WithMessage("expect.fields deve essere un oggetto");
        RuleFor(x => x.Expect).Custom((e, ctx) => ValidateExpectFields(e, ctx));
    }
}

using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace DocflowRules.Api.Validation;

public partial class TestUpsertValidator
{
    static readonly HashSet<string> AllowedRuleKeys = new(StringComparer.OrdinalIgnoreCase) { "equals","approx","regex","exists","tol" };

    private void ValidateExpectFields(JsonObject expect, FluentValidation.ValidationContext<TestUpsertPayload> ctx)
    {
        if (expect is null) { ctx.AddFailure("expect", "expect non può essere nullo"); return; }
        if (expect["fields"] is not JsonObject fields)
        {
            ctx.AddFailure("expect.fields", "expect.fields deve essere un oggetto con i campi attesi");
            return;
        }

        foreach (var kv in fields)
        {
            var field = kv.Key;
            if (kv.Value is not JsonObject rules)
            {
                ctx.AddFailure($"expect.fields.{field}", $"Le regole per il campo '{field}' devono essere un oggetto");
                continue;
            }

            // unknown keys
            foreach (var rk in rules.Select(x => x.Key))
            {
                if (!AllowedRuleKeys.Contains(rk))
                {
                    ctx.AddFailure($"expect.fields.{field}.{rk}", $"Chiave sconosciuta '{rk}'. Consentite: equals, approx, regex, exists, tol");
                }
            }

            // exists
            if (rules.TryGetPropertyValue("exists", out var existsNode))
            {
                if (existsNode is not JsonValue jv || jv.GetValueKind() != System.Text.Json.JsonValueKind.True && jv.GetValueKind() != System.Text.Json.JsonValueKind.False)
                    ctx.AddFailure($"expect.fields.{field}.exists", $"La regola 'exists' deve essere booleana (true/false) per '{field}'");
            }

            // regex
            if (rules.TryGetPropertyValue("regex", out var regexNode))
            {
                if (regexNode is not JsonValue v || v.TryGetValue<string>(out var pattern) is false || string.IsNullOrWhiteSpace(pattern))
                {
                    ctx.AddFailure($"expect.fields.{field}.regex", $"La regola 'regex' deve essere una stringa non vuota per '{field}'");
                }
                else
                {
                    try { _ = new Regex(pattern); }
                    catch (Exception ex) { ctx.AddFailure($"expect.fields.{field}.regex", $"Regex non valida: {ex.Message}"); }
                }
            }

            // approx
            if (rules.TryGetPropertyValue("approx", out var approxNode))
            {
                double? value = null;
                double? tol = null;

                if (approxNode is JsonValue aval && aval.TryGetValue<double>(out var d))
                {
                    value = d;
                    // separate tol allowed
                    if (rules.TryGetPropertyValue("tol", out var tolNode) && tolNode is JsonValue tval && tval.TryGetValue<double>(out var td))
                        tol = td;
                }
                else if (approxNode is JsonObject aobj)
                {
                    if (aobj.TryGetPropertyValue("value", out var v2) && v2 is JsonValue jv2 && jv2.TryGetValue<double>(out var d2))
                        value = d2;
                    if (aobj.TryGetPropertyValue("tol", out var t2) && t2 is JsonValue jv3 && jv3.TryGetValue<double>(out var d3))
                        tol = d3;
                }

                if (value is null)
                    ctx.AddFailure($"expect.fields.{field}.approx", $"La regola 'approx' richiede un numero ('approx': 12.3) o un oggetto {{ value:number, tol?:number }}");
                if (rules.TryGetPropertyValue("tol", out var tolNode2) && tolNode2 is JsonValue tval2 && !tval2.TryGetValue<double>(out _))
                    ctx.AddFailure($"expect.fields.{field}.tol", $"La tolleranza 'tol' deve essere numerica");
            }

            // equals non ha vincoli speciali (accetta qualsiasi JSON), ma avvisa se è vuoto e non ci sono altre regole
            if (!rules.ContainsKey("equals") && !rules.ContainsKey("approx") && !rules.ContainsKey("regex") && !rules.ContainsKey("exists"))
            {
                ctx.AddFailure($"expect.fields.{field}", $"Nessuna regola definita per '{field}'. Usa almeno una tra equals/approx/regex/exists");
            }
        }
    }
}
