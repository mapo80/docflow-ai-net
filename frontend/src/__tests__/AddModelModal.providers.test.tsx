
import { describe, it, expect } from "vitest";
import { render, screen } from "@testing-library/react";
import React from "react";
import AddModelModal from "@/components/models/AddModelModal";

describe("AddModelModal providers", () => {
  it("renders", () => {
    render(<AddModelModal open onCancel={()=>{}} onSubmit={async()=>{}} />);
    expect(screen.getByText(/Add GGUF Model/i)).toBeInTheDocument();
  });
});
