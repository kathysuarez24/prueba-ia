import { test, expect } from '@playwright/test';

test('smoke: API health endpoint responds successfully', async ({ page }) => {
  const baseUrl = process.env.BASE_URL ?? 'http://localhost:5283';

  const response = await page.goto(`${baseUrl}/health`, {
    waitUntil: 'domcontentloaded',
  });

  expect(response).not.toBeNull();
  expect(response!.status()).toBe(200);
  await expect(page.locator('body')).toContainText('AstroBookings');
  await expect(page.locator('body')).toContainText('"status":"ok"');
});
