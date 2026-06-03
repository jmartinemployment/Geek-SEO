const { chromium } = require('playwright');

const TARGET_URL = 'http://localhost:3000';
const BACKEND_URL = 'http://localhost:5051';

const DEV_USER_ID = process.env.NEXT_PUBLIC_DEV_USER_ID || 'test-user-001';

async function testEntityGapsFeature() {
  const browser = await chromium.launch({ headless: false });
  const page = await browser.newPage({ viewport: { width: 1440, height: 900 } });

  const results = {
    passed: [],
    failed: [],
    warnings: [],
  };

  try {
    console.log('🔍 Entity Gaps E2E Test Starting...\n');

    // Step 1: Navigate to topical map page
    console.log('📍 Step 1: Navigating to topical map page...');
    await page.goto(`${TARGET_URL}/app/strategy/topical-map`, { waitUntil: 'domcontentloaded' });
    await page.waitForTimeout(2000);

    const pageTitle = await page.title();
    console.log(`   Page title: ${pageTitle}`);
    results.passed.push('Page loaded');

    // Step 2: Check if at project picker
    const projectPickerVisible = await page.locator('text=Select a project').isVisible().catch(() => false);
    if (projectPickerVisible) {
      console.log('⚠️  At project picker — fetching projects...');

      // Wait for projects to load
      await page.waitForTimeout(2000);

      const projectOptions = await page.locator('[role="option"]').count();
      console.log(`   Found ${projectOptions} projects`);

      if (projectOptions === 0) {
        results.warnings.push('No projects available — cannot test entity gaps');
        await browser.close();
        return results;
      }

      // Click first project
      const firstProject = page.locator('[role="option"]').first();
      await firstProject.click();
      await page.waitForTimeout(2000);
    }

    // Step 3: Check topical map state — should have result or show generate button
    console.log('\n📊 Step 2: Checking topical map state...');

    const generateButtonVisible = await page.locator('button:has-text("Generate")').isVisible().catch(() => false);
    const mapResultVisible = await page.locator('text=Pillars').isVisible().catch(() => false);

    if (generateButtonVisible && !mapResultVisible) {
      console.log('   No map exists — generating via seed mode...');
      results.passed.push('Generate button found');

      // Click generate and wait for dialog
      await page.click('button:has-text("Generate")');
      await page.waitForTimeout(1000);

      // Fill seed keyword
      const seedInput = page.locator('input[placeholder*="seed" i], input[placeholder*="keyword" i]').first();
      const seedInputVisible = await seedInput.isVisible().catch(() => false);

      if (seedInputVisible) {
        await seedInput.fill('digital marketing');
        console.log('   Entered seed keyword: digital marketing');

        // Click generate/submit button
        const submitBtn = page.locator('button:has-text("Generate")').last();
        await submitBtn.click();
        console.log('   Submitted seed mode request...');

        // Wait for results (can take 15-30s for DataForSEO API calls)
        console.log('   Waiting for topical map generation (this may take 15-30s)...');
        await page.waitForTimeout(3000);

        // Poll for results
        let attempts = 0;
        while (attempts < 20) {
          const mapVisible = await page.locator('text=Pillars').isVisible().catch(() => false);
          if (mapVisible) {
            console.log('   ✓ Map generated successfully');
            results.passed.push('Topical map generated via seed mode');
            break;
          }
          await page.waitForTimeout(2000);
          attempts++;
        }

        if (attempts === 20) {
          results.warnings.push('Topical map generation timeout — may still be processing');
        }
      } else {
        console.log('   ⚠️  No seed input found — skipping generation');
        results.warnings.push('Seed input not found');
      }
    } else if (mapResultVisible) {
      console.log('   ✓ Map already exists');
      results.passed.push('Topical map loaded');
    }

    // Step 4: Navigate to Entity Gaps tab
    console.log('\n📑 Step 3: Checking for Entity Gaps tab...');

    await page.waitForTimeout(1000);
    const entityGapsTab = page.locator('[role="tab"]:has-text("Entity Gaps"), button:has-text("Entity Gaps")').first();
    const tabExists = await entityGapsTab.isVisible().catch(() => false);

    if (!tabExists) {
      const allTabs = await page.locator('[role="tab"]').allTextContents();
      console.log(`   ❌ Entity Gaps tab not found`);
      console.log(`   Available tabs: ${allTabs.join(', ')}`);
      results.failed.push('Entity Gaps tab missing from UI');

      await browser.close();
      return results;
    }

    console.log('   ✓ Entity Gaps tab found');
    results.passed.push('Entity Gaps tab exists');

    // Click the tab
    await entityGapsTab.click();
    await page.waitForTimeout(1500);
    console.log('   Clicked Entity Gaps tab');

    // Step 5: Verify table structure
    console.log('\n🔍 Step 4: Validating Entity Gaps table...');

    const tableVisible = await page.locator('table, [role="grid"]').first().isVisible().catch(() => false);
    if (!tableVisible) {
      results.failed.push('No table found in Entity Gaps tab');
    } else {
      results.passed.push('Entity Gaps table rendered');
      console.log('   ✓ Table found');

      // Check for key columns
      const rowCount = await page.locator('tbody tr, [role="row"]').count().catch(() => 0);
      console.log(`   Rows: ${rowCount}`);

      if (rowCount > 0) {
        results.passed.push(`Entity Gaps table has ${rowCount} rows`);

        // Sample first row — check for expected columns
        const firstRow = page.locator('tbody tr, [role="row"]').first();
        const rowText = await firstRow.textContent().catch(() => '');
        console.log(`   First row content (preview): ${rowText.substring(0, 100)}...`);

        // Check for entityCoverage percentage or gap count
        const hasPercentage = /\d+%/.test(rowText);
        const hasGapInfo = /gap|missing|entity/i.test(rowText);

        if (hasPercentage) {
          results.passed.push('Entity coverage percentages displayed');
          console.log('   ✓ Coverage percentages found');
        } else {
          results.warnings.push('Coverage percentages not visible in first row');
        }

        if (hasGapInfo) {
          results.passed.push('Gap information displayed');
          console.log('   ✓ Gap information found');
        }

        // Check for badges (missing entities)
        const badges = await page.locator('[class*="badge"], [class*="tag"], span[class*="inline"]').count().catch(() => 0);
        if (badges > 0) {
          results.passed.push(`${badges} entity badges found`);
          console.log(`   ✓ Entity badges: ${badges}`);
        }
      } else {
        results.warnings.push('Entity Gaps table is empty — no data to verify');
        console.log('   ⚠️  Table is empty');
      }
    }

    // Step 6: Screenshot for visual verification
    console.log('\n📸 Capturing screenshot...');
    await page.screenshot({
      path: '/tmp/entity-gaps-test-result.png',
      fullPage: false,
    });
    console.log('   Screenshot saved to /tmp/entity-gaps-test-result.png');
    results.passed.push('Screenshot captured');

  } catch (error) {
    console.error('\n❌ Test error:', error.message);
    results.failed.push(`Test error: ${error.message}`);
  } finally {
    await browser.close();
  }

  // Print results summary
  console.log('\n' + '='.repeat(60));
  console.log('TEST RESULTS SUMMARY');
  console.log('='.repeat(60));
  console.log(`✓ Passed: ${results.passed.length}`);
  results.passed.forEach((p) => console.log(`  • ${p}`));

  if (results.warnings.length > 0) {
    console.log(`⚠️  Warnings: ${results.warnings.length}`);
    results.warnings.forEach((w) => console.log(`  • ${w}`));
  }

  if (results.failed.length > 0) {
    console.log(`❌ Failed: ${results.failed.length}`);
    results.failed.forEach((f) => console.log(`  • ${f}`));
  }

  console.log('='.repeat(60));

  const overallStatus = results.failed.length === 0 ? '✅ PASS' : '❌ FAIL';
  console.log(`\nOverall: ${overallStatus}`);

  process.exit(results.failed.length === 0 ? 0 : 1);
}

testEntityGapsFeature().catch((err) => {
  console.error('Fatal error:', err);
  process.exit(1);
});
