import { describe, it, expect, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import React from "react";
import TemplatesPage from "@/pages/TemplatesPage";

describe("TemplatesPage", () => {
  it("renders", async () => {
    vi.spyOn(global, "fetch").mockResolvedValueOnce({ ok: true, json: async () => [] } as any);
    render(<TemplatesPage />);
    await waitFor(() => expect(screen.getByText(/Templates/i)).toBeInTheDocument());
  });
});
