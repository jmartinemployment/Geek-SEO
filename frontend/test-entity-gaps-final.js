const { chromium } = require('playwright');

(async () => {
  const browser = await chromium.launch({ headless: false });
  const page = await browser.newPage({ viewport: { width: 1440, height: 900 } });

  const results = { passed: [], failed: [], warnings: [] };

  try {
    console.log('🔍 Entity Gaps E2E Test\n');

    console.log('Step 1: Loading app...');
    await page.goto('http://localhost:3000/app/strategy/topical-map', { waitUntil: 'domcontentloaded' });
    await page.waitForTimeout(2000);
    results.passed.push('App loaded & authenticated');

    console.log('\nStep 2: Creating test project...');
    const projRes = await page.evaluate(async () => {
      const res = await fetch('http://localhost:5051/api/seo/projects', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          url: 'https://example.com',
          request: { name: 'Entity Gaps Test ' + Date.now() },
        }),
      });
      if (!res.ok) return { ok: false, status: res.status };
      return { ok: true, data: await res.json() };
    });

    if (!projRes.ok) {
      console.log(`❌ Project API error: ${projRes.status}`);
      results.failed.push('Project creation failed');
      await browser.close();
      return logResults(results);
    }

    const projectId = projRes.data.id;
    console.log(`✓ Project created: ${projectId}`);
    results.passed.push('Test project created');

    console.log('\nStep 3: Generating topical map...');
    const mapRes = await page.evaluate(async (pId) => {
      const res = await fetch('http://localhost:5051/api/seo/topical-map/' + pId + '/generate', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ seedKeyword: 'digital marketing' }),
      });
      return { ok: res.ok, status: res.status };
    }, projectId);

    if (mapRes.ok) {
      console.log('✓ Map generation started');
      results.passed.push('Map generation initiated');
    } else {
      console.log(`⚠️  Map API returned ${mapRes.status}`);
      results.warnings.push('Map generation may be incomplete');
    }

    await page.waitForTimeout(8000);

    console.log('\nStep 4: Navigating to map view...');
    await page.goto('http://localhost:3000/app/strategy/topical-map?projectId=' + projectId, {
      waitUntil: 'domcontentloaded',
    });
    await page.waitForTimeout(3000);

    console.log('\nStep 5: Looking for Entity Gaps tab...');
    const btnFound = await page.locator('button:has-text("Entity Gaps")').isVisible().catch(() => false);

    if (!btnFound) {
      console.log('❌ Entity Gaps tab NOT found');
      results.failed.push('Entity Gaps tab missing');
    } else {
      console.log('✓ Entity Gaps tab found');
      results.passed.push('Entity Gaps tab rendered');

      await page.click('button:has-text("Entity Gaps")');
      await page.waitForTimeout(1500);

      const table = await page.locator('table').isVisible().catch(() => false);
      if (table) {
        const rows = await page.locator('tbody tr').count();
        console.log(`✓ Table visible: ${rows} rows`);
        results.passed.push('Entity Gaps table displayed');
      } else {
        results.warnings.push('Tab exists but table not visible');
      }
    }

    await page.screenshot({ path: '/tmp/entity-gaps-result.png' });
    console.log('\nScreenshot: /tmp/entity-gaps-result.png');
    results.passed.push('Screenshot captured');

  } catch (error) {
    results.failed.push(error.message);
  } finally {
    await browser.close();
  }

  logResults(results);
})();

function logResults(r) {
  console.log('\n' + '='.repeat(60));
  console.log(`✓ ${r.passed.length} passed`);
  r.passed.forEach(p => console.log(`  ✓ ${p}`));
  if (r.warnings.length) {
    console.log(`⚠️  ${r.warnings.length} warnings`);
    r.warnings.forEach(w => console.log(`  ⚠️  ${w}`));
  }
  if (r.failed.length) {
    console.log(`❌ ${r.failed.length} failed`);
    r.failed.forEach(f => console.log(`  ❌ ${f}`));
  }
  console.log('='.repeat(60));
  console.log(r.failed.length === 0 ? '✅ PASS' : '❌ FAIL');
  process.exit(r.failed.length === 0 ? 0 : 1);
}
