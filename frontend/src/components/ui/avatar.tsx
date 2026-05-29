import * as React from 'react';
import { cn } from '@/lib/utils';

function Avatar({ className, ...props }: React.ComponentProps<'span'>) {
  return (
    <span
      className={cn(
        'relative flex size-8 shrink-0 overflow-hidden rounded-full bg-[var(--color-surface-muted)]',
        className,
      )}
      {...props}
    />
  );
}

function AvatarImage({ className, alt, ...props }: React.ComponentProps<'img'>) {
  return <img className={cn('aspect-square size-full object-cover', className)} alt={alt} {...props} />;
}

function AvatarFallback({ className, ...props }: React.ComponentProps<'span'>) {
  return (
    <span
      className={cn(
        'flex size-full items-center justify-center bg-[var(--color-accent)] text-xs font-semibold text-white',
        className,
      )}
      {...props}
    />
  );
}

export { Avatar, AvatarImage, AvatarFallback };
