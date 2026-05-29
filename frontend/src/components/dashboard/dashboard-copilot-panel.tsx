'use client';

import Link from 'next/link';
import { ChevronDown, Sparkles } from 'lucide-react';
import { useState } from 'react';
import type { CopilotSuggestion } from '@/lib/dashboard-data';
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';

export function DashboardCopilotPanel({ suggestions }: { suggestions: CopilotSuggestion[] }) {
  const [open, setOpen] = useState(true);

  return (
    <Card className="overflow-hidden">
      <CardHeader className="flex-row items-center justify-between space-y-0 pb-0">
        <div className="flex items-center gap-2">
          <div className="flex size-8 items-center justify-center rounded-lg bg-[rgba(124,58,237,0.12)] text-[var(--color-badge-purple)]">
            <Sparkles className="size-4" />
          </div>
          <div>
            <CardTitle>CopilotAI</CardTitle>
            <p className="text-xs text-[var(--color-text-secondary)]">Your personal recommendations</p>
          </div>
          <Badge variant="purple">For you</Badge>
        </div>
        <Button variant="ghost" size="sm" onClick={() => setOpen((value) => !value)}>
          {open ? 'Collapse' : 'Open'}
          <ChevronDown className={open ? 'rotate-180' : ''} />
        </Button>
      </CardHeader>
      {open ? (
        <CardContent className="flex flex-col gap-3 pt-4">
          {suggestions.map((suggestion) => (
            <Alert key={suggestion.id}>
              <AlertTitle>{suggestion.title}</AlertTitle>
              <AlertDescription className="flex flex-col gap-3 pt-1 sm:flex-row sm:items-center sm:justify-between">
                <span>{suggestion.detail}</span>
                <Link
                  href={suggestion.href}
                  className="shrink-0 text-sm font-semibold text-[var(--color-accent)] hover:text-[var(--color-accent-hover)]"
                >
                  Review →
                </Link>
              </AlertDescription>
            </Alert>
          ))}
        </CardContent>
      ) : null}
    </Card>
  );
}
