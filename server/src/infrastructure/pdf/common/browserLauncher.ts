import puppeteer from 'puppeteer';

/**
 * Launch Puppeteer browser with common configuration
 */
export async function launchBrowser() {
  return puppeteer.launch({
    executablePath: '/usr/bin/chromium',
    headless: 'new',
    args: ['--no-sandbox', '--disable-setuid-sandbox'],
  });
}

