import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import React from "react";
import ModelsPage from "@/pages/ModelsPage";

// rudimentary test to ensure page renders without crashing
describe("ModelsPage", () => {
  beforeEach(() => {
    vi.spyOn(global, "fetch").mockResolvedValue({
      ok: true,
      json: async () => [],
    } as any);
  });

  it("renders title", async () => {
    render(<ModelsPage />);
    await waitFor(() => {
      expect(screen.getByText(/Models/i)).toBeInTheDocument();
    });
  });
});
