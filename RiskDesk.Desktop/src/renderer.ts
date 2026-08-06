/**
 * This file will automatically be loaded by webpack and run in the "renderer" context.
 * To learn more about the differences between the "main" and the "renderer" context in
 * Electron, visit:
 *
 * https://electronjs.org/docs/latest/tutorial/process-model
 *
 * By default, Node.js integration in this file is disabled. When enabling Node.js integration
 * in a renderer process, please be aware of potential security implications. You can read
 * more about security risks here:
 *
 * https://electronjs.org/docs/tutorial/security
 *
 * To enable Node.js integration in this file, open up `main.js` and enable the `nodeIntegration`
 * flag:
 *
 * ```
 *  // Create the browser window.
 *  mainWindow = new BrowserWindow({
 *    width: 800,
 *    height: 600,
 *    webPreferences: {
 *      nodeIntegration: true
 *    }
 *  });
 * ```
 */

import './index.css';

const button = document.querySelector('#load-landfalls');
const summary = document.querySelector('#summary');
const results = document.querySelector('#results');

button?.addEventListener('click', async () => {
  try {
    const response = await fetch('http://127.0.0.1:5202/api/landfalls');
    const report = await response.json();
    if (summary) {
      summary.textContent =
        `Storms analyzed: ${report.stormCount} | Florida landfalls: ${report.landfallCount}`;
    }

    if (results) {
      results.textContent = report.events
        .map(
          (event: {
            stormName: string;
            landfallTimeUtc: string;
            landfallWindSpeedKnots: number;
          }) =>
            `${event.stormName} — ${event.landfallTimeUtc} — ${event.landfallWindSpeedKnots} kt`,
        )
        .join('\n');
    }

    console.log(report);
  } catch (error) {
    console.error('Could not load landfalls:', error);
  }

});

