import * as React from 'react';
import { cva, type VariantProps } from 'class-variance-authority';
import { cn } from '@/lib/utils';

const badgeVariants = cva(
  'inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium',
  {
    variants: {
      variant: {
        default: 'bg-[var(--color-surface-muted)] text-[var(--color-text-secondary)]',
        accent: 'bg-[rgba(59,179,122,0.12)] text-[var(--color-accent)]',
        purple: 'bg-[rgba(124,58,237,0.12)] text-[var(--color-badge-purple)]',
        score: 'bg-[var(--color-surface-muted)] text-[var(--color-metric-blue)] font-semibold',
      },
    },
    defaultVariants: {
      variant: 'default',
    },
  },
);

function Badge({
  className,
  variant,
  ...props
}: React.ComponentProps<'span'> & VariantProps<typeof badgeVariants>) {
  return <span className={cn(badgeVariants({ variant, className }))} {...props} />;
}

export { Badge, badgeVariants };
