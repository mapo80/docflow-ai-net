
import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen } from "@testing-library/react";
import React from "react";
import JobNew from "@/pages/JobNew";
import { MemoryRouter } from "react-router-dom";

describe("JobNew", () => {
  beforeEach(() => {
    vi.spyOn(global, "fetch").mockResolvedValue({ ok: true, json: async () => [] } as any);
  });
  it("renders", () => {
    render(<MemoryRouter><JobNew /></MemoryRouter>);
    expect(screen.getByText(/New Job/i)).toBeInTheDocument();
    expect(screen.getAllByText(/Template/i)[0]).toBeInTheDocument();
  });
});
