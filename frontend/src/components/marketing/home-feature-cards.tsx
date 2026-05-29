import Link from 'next/link';
import { FEATURE_MODULES } from '@/components/dashboard/dashboard.constants';

export function HomeFeatureCards() {
  return (
    <div>
      <h2 className="text-center text-sm font-semibold uppercase tracking-[0.08em] text-[var(--color-text-secondary)]">
        Your edge to win every search
      </h2>
      <div className="mt-6 flex gap-3 overflow-x-auto pb-2">
        {FEATURE_MODULES.map((module) => (
          <Link
            key={module.id}
            href="/api/auth/start"
            className="group flex w-40 shrink-0 flex-col rounded-[var(--radius-card)] border border-[var(--color-border)] bg-white p-4 shadow-[var(--shadow-card)] transition-[border-color,box-shadow] hover:border-[var(--color-border-strong)] hover:shadow-[var(--shadow-card-hover)]"
          >
            <div
              className="flex size-10 items-center justify-center rounded-xl"
              style={{ backgroundColor: module.iconBg, color: module.iconColor }}
            >
              <span className="text-sm font-bold">{module.title.charAt(0)}</span>
            </div>
            <p className="mt-3 text-[15px] font-semibold leading-tight text-[var(--color-text-primary)]">
              {module.title}
            </p>
            <p className="mt-2 line-clamp-2 text-xs leading-5 text-[var(--color-text-secondary)]">
              {module.description}
            </p>
          </Link>
        ))}
      </div>
    </div>
  );
}
