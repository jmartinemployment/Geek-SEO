import * as React from 'react';
import { cn } from '@/lib/utils';

function Empty({ className, ...props }: React.ComponentProps<'div'>) {
  return (
    <div
      className={cn(
        'flex min-w-0 flex-1 flex-col items-center justify-center rounded-[var(--radius-card)] border border-dashed border-[var(--color-border)] bg-[var(--color-bg)] px-6 py-10 text-center',
        className,
      )}
      {...props}
    />
  );
}

function EmptyHeader({ className, ...props }: React.ComponentProps<'div'>) {
  return <div className={cn('flex max-w-sm flex-col items-center gap-2', className)} {...props} />;
}

function EmptyMedia({ className, ...props }: React.ComponentProps<'div'>) {
  return (
    <div
      className={cn(
        'flex size-12 items-center justify-center rounded-full bg-white text-[var(--color-text-secondary)] shadow-[var(--shadow-card)]',
        className,
      )}
      {...props}
    />
  );
}

function EmptyTitle({ className, ...props }: React.ComponentProps<'h3'>) {
  return <h3 className={cn('text-base font-semibold text-[var(--color-text-primary)]', className)} {...props} />;
}

function EmptyDescription({ className, ...props }: React.ComponentProps<'p'>) {
  return <p className={cn('text-sm text-[var(--color-text-secondary)]', className)} {...props} />;
}

function EmptyContent({ className, ...props }: React.ComponentProps<'div'>) {
  return <div className={cn('mt-4 flex flex-col gap-2', className)} {...props} />;
}

export { Empty, EmptyHeader, EmptyMedia, EmptyTitle, EmptyDescription, EmptyContent };
