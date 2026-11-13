const { chromium } = require('playwright');
const assert = require('node:assert/strict');
const fs = require('node:fs/promises');

(async () => {
  const browser = await chromium.launch({ executablePath: process.env.CHROMIUM_PATH || undefined });
  const page = await browser.newPage({ acceptDownloads: true });
  const failures = [];
  page.on('pageerror', error => failures.push(error.message));
  const baseUrl = process.env.CRS_BASE_URL || 'http://localhost:5174';
  const passed = [];
  try {
    const response = await page.goto(baseUrl + '/CallList');
    assert.equal(response.headers()['x-crs-test-host'], 'true', 'Run the browser tests only against the test-project host');
    await page.getByRole('heading', { name: 'My calls', exact: true }).waitFor();
    assert.equal(await page.getByRole('button', { name: 'Menu', exact: true }).isVisible(), false);
    passed.push('test-only fixtures render call information cards');

    await page.route('**/CallList/Rate?**', route => route.abort());
    await page.locator('.call-select').first().click();
    await page.getByText('Call details could not be loaded. Close this popup and try again.', { exact: true }).waitFor();
    await page.getByRole('dialog').getByRole('button', { name: 'Close', exact: true }).first().click();
    await page.getByRole('dialog').waitFor({ state: 'hidden' });
    await page.unroute('**/CallList/Rate?**');
    passed.push('popup loading failure can be closed and retried');

    await page.locator('.call-select').first().click();
    await page.getByRole('dialog').waitFor();
    assert.equal(new URL(page.url()).pathname, '/CallList', 'Selecting a card must open a popup without navigating');
    // Submit without choosing a problem: server-side validation must preserve the form.
    await page.getByLabel('Pharmacy', { exact: true }).selectOption('1');
    await page.getByRole('button', { name: 'Save rating and notify help desk' }).click();
    await page.getByText('Select at least one problem.', { exact: true }).waitFor();
    await page.getByLabel('Poor audio', { exact: true }).check();
    await page.getByLabel('Other details', { exact: true }).fill('Browser smoke test');
    await page.route('**/CallList/Rate', route => route.abort());
    await page.getByRole('button', { name: 'Save rating and notify help desk' }).click();
    await page.getByText('The save could not be confirmed.', { exact: false }).waitFor();
    assert.equal(await page.getByLabel('Other details', { exact: true }).inputValue(), 'Browser smoke test');
    await page.unroute('**/CallList/Rate');
    passed.push('popup save failure preserves entered data and permits retry');
    await page.getByRole('button', { name: 'Save rating and notify help desk' }).click();
    await page.getByText('Call rating saved and help desk notified.', { exact: true }).waitFor();
    assert.equal(await page.getByRole('dialog').count(), 0);
    passed.push('call card opens popup; validation stays in popup; save returns to cards');

    await page.locator('.call-select').first().click();
    await page.getByRole('dialog').waitFor();
    assert.equal(new URL(page.url()).pathname, '/CallList', 'Selecting a card must open a popup without navigating');
    await page.getByLabel('Poor audio', { exact: true }).check();
    await page.getByLabel('Pharmacy', { exact: true }).selectOption('1');
    await page.getByRole('button', { name: 'Save rating and notify help desk' }).click();
    await page.getByText('This call has already been reported. No duplicate was saved.', { exact: true }).waitFor();
    passed.push('duplicate submission protection');

    const callDownloadEvent = page.waitForEvent('download');
    await page.getByRole('link', { name: 'Recording', exact: true }).first().click();
    await callDownloadEvent;
    assert.equal(await page.getByRole('dialog').count(), 0, 'Download must not select the card');
    await page.locator('.call-select').first().focus();
    await page.keyboard.press('Enter');
    await page.getByRole('dialog').waitFor();
    await page.getByRole('dialog').getByRole('button', { name: 'Close', exact: true }).last().click();
    await page.getByRole('dialog').waitFor({ state: 'hidden' });
    passed.push('separate card download; keyboard selection and popup cancellation');
    await page.getByRole('link', { name: 'Help desk', exact: true }).click();
    await page.getByRole('heading', { name: 'Help desk', exact: true }).waitFor();
    await page.getByRole('link', { name: 'Next', exact: true }).click();
    await page.getByText('Page 2 of 3', { exact: true }).waitFor();
    await page.getByRole('link', { name: 'Previous', exact: true }).click();
    await page.getByLabel('Status', { exact: true }).selectOption('1');
    await page.getByRole('button', { name: 'Apply filter', exact: true }).click();
    await page.getByRole('link', { name: 'Review', exact: true }).first().click();
    await page.getByLabel('Help-desk status', { exact: true }).selectOption('2');
    await page.getByLabel('Help-desk reason', { exact: true }).selectOption('1');
    await page.getByLabel('IT note', { exact: true }).fill('Browser confirmed resolution');
    await page.getByRole('button', { name: 'Save update', exact: true }).click();
    await page.getByText('Help-desk update saved and notifications sent.', { exact: true }).waitFor();
    passed.push('help-desk paging, filtering, review and update');

    const reportEvent = page.waitForEvent('download');
    await page.getByRole('link', { name: 'Export Excel', exact: true }).click();
    const report = await reportEvent;
    assert.equal(report.suggestedFilename(), 'CallRatings.xlsx');
    assert.ok((await fs.stat(await report.path())).size > 1000);
    passed.push('Excel report download');

    await page.getByRole('link', { name: 'Review', exact: true }).first().click();
    const audioEvent = page.waitForEvent('download');
    await page.getByRole('link', { name: 'Download recording', exact: true }).click();
    const audio = await audioEvent;
    const bytes = await fs.readFile(await audio.path());
    assert.equal(bytes.subarray(0, 4).toString(), 'RIFF');
    passed.push('recording download');
    if (process.env.SCREENSHOT_PATH) await page.screenshot({ path: process.env.SCREENSHOT_PATH, fullPage: true });
    await page.setViewportSize({ width: 390, height: 844 });
    await page.goto(baseUrl + '/CallList');
    await page.getByRole('button', { name: 'Menu', exact: true }).click();
    await page.getByRole('link', { name: 'Help desk', exact: true }).waitFor();
    assert.equal(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth), true, 'Mobile page must not overflow horizontally');
    passed.push('mobile legacy theme and navigation');
    assert.deepEqual(failures, [], 'Browser JavaScript errors');
    console.log(JSON.stringify({ passed, javascriptErrors: failures }, null, 2));
  } finally { await browser.close(); }
})().catch(error => { console.error(error); process.exitCode = 1; });
