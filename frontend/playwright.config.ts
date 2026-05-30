import path from 'node:path';
import { defineConfig, devices } from '@playwright/test';

const baseURL =
  process.env.PLAYWRIGHT_BASE_URL?.replace(/\/$/u, '') ?? 'https://seo.geekatyourspot.com';

const authFile = path.join(__dirname, 'e2e/.auth/user.json');
const hasAuthCredentials = Boolean(
  process.env.PLAYWRIGHT_TEST_EMAIL?.trim() && process.env.PLAYWRIGHT_TEST_PASSWORD,
);

export default defineConfig({
  testDir: './e2e',
  globalSetup: './e2e/global-setup.ts',
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: process.env.CI ? 1 : undefined,
  reporter: process.env.CI ? [['list'], ['html', { open: 'never' }]] : 'list',
  timeout: 60_000,
  use: {
    baseURL,
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    video: 'off',
  },
  projects: [
    {
      name: 'smoke',
      testMatch: /smoke\.spec\.ts/,
      use: { ...devices['Desktop Chrome'] },
    },
    ...(hasAuthCredentials
      ? [
          {
            name: 'authenticated',
            testMatch: /authenticated\.spec\.ts/,
            use: {
              ...devices['Desktop Chrome'],
              storageState: authFile,
            },
          },
        ]
      : []),
  ],
});
