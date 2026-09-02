"use client";

import * as React from "react";
import { Check } from "lucide-react";
import { cn } from "@/lib/utils";

export interface CheckboxProps {
  checked?: boolean;
  onCheckedChange?: (checked: boolean) => void;
  disabled?: boolean;
  className?: string;
  id?: string;
}

export function Checkbox({
  checked = false,
  onCheckedChange,
  disabled = false,
  className,
  id,
}: CheckboxProps) {
  return (
    <button
      type="button"
      role="checkbox"
      aria-checked={checked}
      disabled={disabled}
      id={id}
      onClick={() => onCheckedChange?.(!checked)}
      className={cn(
        "peer size-4 shrink-0 rounded-xs border transition-all duration-150 flex items-center justify-center cursor-pointer",
        checked
          ? "bg-[#D81B60] border-[#D81B60] text-white shadow-xs"
          : "border-slate-300 bg-white hover:border-slate-400 dark:border-slate-600 dark:bg-slate-900",
        disabled && "cursor-not-allowed opacity-50",
        className
      )}
    >
      {checked && <Check className="size-3 stroke-[3]" />}
    </button>
  );
}
