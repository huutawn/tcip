import * as React from "react";
import { cva, type VariantProps } from "class-variance-authority";
import { cn } from "@/lib/utils";

const badgeVariants = cva(
  "inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-semibold transition-colors focus:outline-hidden",
  {
    variants: {
      variant: {
        default: "border-transparent bg-blue-600 text-white shadow-xs",
        secondary: "border-transparent bg-slate-100 text-slate-900 dark:bg-slate-800 dark:text-slate-100",
        destructive: "border-transparent bg-red-500 text-white shadow-xs",
        outline: "text-slate-950 border border-slate-200 dark:border-slate-800 dark:text-slate-50",
        success: "border-transparent bg-emerald-500 text-white shadow-xs",
        warning: "border-transparent bg-amber-500 text-white shadow-xs",
      },
    },
    defaultVariants: {
      variant: "default",
    },
  }
);

export interface BadgeProps
  extends React.HTMLAttributes<HTMLDivElement>,
    VariantProps<typeof badgeVariants> {}

function Badge({ className, variant, ...props }: BadgeProps) {
  return (
    <div className={cn(badgeVariants({ variant }), className)} {...props} />
  );
}

export { Badge, badgeVariants };
