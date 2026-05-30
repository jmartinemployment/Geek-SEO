import fs from 'node:fs';
import path from 'node:path';
import { chromium, type FullConfig } from '@playwright/test';
import { getTestCredentials, isDevUserMode, loginViaGeekOAuth } from './auth-helpers';

const authDir = path.join(__dirname, '.auth');
const authFile = path.join(authDir, 'user.json');

export default async function globalSetup(config: FullConfig) {
  if (isDevUserMode()) {
    return;
  }

  const credentials = getTestCredentials();

  if (!credentials) {
    if (fs.existsSync(authFile)) {
      fs.unlinkSync(authFile);
    }
    return;
  }

  const baseURL =
    (config.projects[0]?.use?.baseURL as string | undefined) ??
    process.env.PLAYWRIGHT_BASE_URL ??
    'https://seo.geekatyourspot.com';

  fs.mkdirSync(authDir, { recursive: true });

  const browser = await chromium.launch();
  const context = await browser.newContext();
  const page = await context.newPage();

  await loginViaGeekOAuth(page, baseURL.replace(/\/$/u, ''), credentials);
  await context.storageState({ path: authFile });
  await browser.close();
}
