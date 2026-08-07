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
const stormCount = document.querySelector('#storm-count');
const landfallCount = document.querySelector('#landfall-count');
const resultsBody = document.querySelector('#results-body');
const results = document.querySelector('#results');
const errorMessage = document.querySelector('#error-message');

type LandfallEvent = {
  stormId: string;
  stormName: string;
  landfallTimeUtc: string;
  landfallWindSpeedKnots: number;
  maxFloridaWindSpeedKnots: number;
};

button?.addEventListener('click', async () => {
  try {
    button?.setAttribute('disabled', 'true');
    button?.replaceChildren(document.createTextNode('Analyzing...'));
    errorMessage?.setAttribute('hidden', 'true');
    errorMessage?.replaceChildren();
    const response = await fetch('http://127.0.0.1:5202/api/landfalls');
    const report = await response.json();
    if (stormCount) {
      stormCount.textContent = String(report.stormCount);
    }

    if (landfallCount) {
      landfallCount.textContent = String(report.landfallCount);
    }

    resultsBody?.replaceChildren();

    const stormGroups = new Map<string, number>();
    let nextGroup = 0;

    report.events.forEach((event: LandfallEvent) => {
      const row = document.createElement('tr');
      if (!stormGroups.has(event.stormId)) {
        stormGroups.set(event.stormId, nextGroup++);
      }

      const groupIndex = stormGroups.get(event.stormId) ?? 0;
      row.className =
        groupIndex % 2 === 0 ? 'storm-group-even' : 'storm-group-odd';

      const values = [
        event.stormName,
        new Date(event.landfallTimeUtc).toLocaleDateString(),
        `${event.landfallWindSpeedKnots} kt`,
        `${event.maxFloridaWindSpeedKnots} kt`,
      ];

      values.forEach((value) => {
        const cell = document.createElement('td');
        cell.textContent = value;
        row.appendChild(cell);
      });

      resultsBody?.appendChild(row);
    });

    results?.removeAttribute('hidden');

    console.log(report);
  } catch (error) {
    if (errorMessage) {
      errorMessage.textContent =
        'Unable to load landfall data. Is the API running?';
      errorMessage.removeAttribute('hidden');
    }
    console.error('Could not load landfalls:', error);
  } finally {
    button?.removeAttribute('disabled');
    button?.replaceChildren(
      document.createTextNode('Load Florida Landfalls'),
    );
  }


});
