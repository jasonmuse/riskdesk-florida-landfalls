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
const stormFilter = document.querySelector<HTMLInputElement>('#storm-filter');
const visibleCount = document.querySelector('#visible-count');
const sortButtons = document.querySelectorAll<HTMLButtonElement>('.sort-button');

type SortKey =
  | 'stormName'
  | 'landfallTimeUtc'
  | 'landfallWindSpeedKnots'
  | 'maxFloridaWindSpeedKnots';

type SortDirection = 'asc' | 'desc';

function rollNumber(element: Element | null, target: number) {
  if (!element) return;

  const start = Number(element.textContent) || 0;
  const duration = 700;
  const startedAt = performance.now();
  element.classList.remove('number-rolling');
  void element.clientWidth;
  element.classList.add('number-rolling');

  const update = (now: number) => {
    const progress = Math.min((now - startedAt) / duration, 1);
    const eased = 1 - Math.pow(1 - progress, 3);
    element.textContent = String(Math.round(start + (target - start) * eased));
    if (progress < 1) requestAnimationFrame(update);
  };

  requestAnimationFrame(update);
}

function setButtonLabel(label: string) {
  if (!button) return;
  const labelElement = document.createElement('span');
  labelElement.className = 'button-label';
  labelElement.textContent = label;
  button.replaceChildren(labelElement);
}

type LandfallEvent = {
  stormId: string;
  stormName: string;
  landfallTimeUtc: string;
  landfallWindSpeedKnots: number;
  maxFloridaWindSpeedKnots: number;
};

let loadedEvents: LandfallEvent[] = [];
let sortKey: SortKey | null = null;
let sortDirection: SortDirection = 'asc';

function updateSortButtons() {
  sortButtons.forEach((sortButton) => {
    const buttonSortKey = sortButton.dataset.sortKey as SortKey;
    const label = sortButton.dataset.sortLabel ?? 'column';
    const isActive = buttonSortKey === sortKey && loadedEvents.length > 0;

    sortButton.classList.toggle('is-active', isActive);
    sortButton.dataset.direction = isActive ? sortDirection : 'none';

    const nextDirection = isActive && sortDirection === 'asc'
      ? 'descending'
      : 'ascending';
    const tooltip = isActive
      ? `Sorted by ${label} ${sortDirection === 'asc' ? 'ascending' : 'descending'}. Click to sort ${nextDirection}.`
      : `Sort by ${label} ascending.`;

    sortButton.title = tooltip;
    sortButton.setAttribute('aria-label', tooltip);
  });
}

function compareEvents(
  firstEvent: LandfallEvent,
  secondEvent: LandfallEvent,
) {
  if (!sortKey) return 0;

  const firstValue = firstEvent[sortKey];
  const secondValue = secondEvent[sortKey];

  if (typeof firstValue === 'number' && typeof secondValue === 'number') {
    return firstValue - secondValue;
  }

  return String(firstValue).localeCompare(String(secondValue));
}

function renderEvents(events: LandfallEvent[], animateSort = false) {
  resultsBody?.replaceChildren();

  if (visibleCount) {
    visibleCount.textContent = `Showing ${events.length} of ${loadedEvents.length} landfalls`;
  }

  if (events.length === 0) {
    const row = document.createElement('tr');
    const cell = document.createElement('td');
    cell.className = 'empty-results';
    cell.colSpan = 4;
    cell.textContent = 'No storms match that search.';
    row.appendChild(cell);
    resultsBody?.appendChild(row);
    return;
  }

  const stormGroups = new Map<string, number>();
  let nextGroup = 0;

  events.forEach((event, eventIndex) => {
    const row = document.createElement('tr');

    if (animateSort) {
      row.classList.add('sort-reveal');
      row.style.animationDelay = `${Math.min(eventIndex, 10) * 18}ms`;
    }

    if (!stormGroups.has(event.stormId)) {
      stormGroups.set(event.stormId, nextGroup++);
    }

    const groupIndex = stormGroups.get(event.stormId) ?? 0;
    row.classList.add(
      groupIndex % 2 === 0 ? 'storm-group-even' : 'storm-group-odd',
    );

    const values = [
      event.stormName,
      new Date(event.landfallTimeUtc).toLocaleDateString(undefined, {
        timeZone: 'UTC',
      }),
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
}

function renderSortedEvents(animateSort = false) {
  const query = stormFilter?.value.trim().toLocaleLowerCase() ?? '';
  const filteredEvents = query
    ? loadedEvents.filter((event) =>
      event.stormName.toLocaleLowerCase().includes(query))
    : loadedEvents;
  const sortedEvents = sortKey
    ? [...filteredEvents].sort(compareEvents)
    : [...filteredEvents];

  if (sortDirection === 'desc') {
    sortedEvents.reverse();
  }

  renderEvents(sortedEvents, animateSort);
  updateSortButtons();
}

sortButtons.forEach((sortButton) => {
  sortButton.addEventListener('click', () => {
    const nextSortKey = sortButton.getAttribute('data-sort-key') as SortKey;
    if (!nextSortKey || loadedEvents.length === 0) return;

    if (sortKey === nextSortKey) {
      sortDirection = sortDirection === 'asc' ? 'desc' : 'asc';
    } else {
      sortKey = nextSortKey;
      sortDirection = 'asc';
    }

    renderSortedEvents(true);
  });
});

updateSortButtons();

stormFilter?.addEventListener('input', () => {
  renderSortedEvents(true);
});

button?.addEventListener('click', async () => {
  try {
    button?.setAttribute('disabled', 'true');
    button?.classList.add('is-loading');
    if (stormCount) {
      stormCount.textContent = '-';
      stormCount.classList.add('value-loading');
    }
    if (landfallCount) {
      landfallCount.textContent = '-';
      landfallCount.classList.add('value-loading');
    }
    setButtonLabel('Analyzing...');
    errorMessage?.setAttribute('hidden', 'true');
    errorMessage?.replaceChildren();
    const response = await fetch('http://127.0.0.1:5202/api/landfalls');

    if (!response.ok) {
      throw new Error(`The API returned ${response.status}.`);
    }

    const report = await response.json();
    if (stormCount) {
      stormCount.classList.remove('value-loading');
      rollNumber(stormCount, report.stormCount);
    }

    if (landfallCount) {
      landfallCount.classList.remove('value-loading');
      rollNumber(landfallCount, report.landfallCount);
    }

    loadedEvents = report.events;
    sortKey = null;
    sortDirection = 'asc';
    renderSortedEvents();

    const resultsWereHidden = results?.hasAttribute('hidden') ?? true;
    results?.removeAttribute('hidden');
    if (resultsWereHidden) {
      results?.classList.remove('results-reveal');
      void results?.clientWidth;
      results?.classList.add('results-reveal');
    }

  } catch (error) {
    if (errorMessage) {
      errorMessage.textContent =
        'Unable to load landfall data. Is the API running?';
      errorMessage.removeAttribute('hidden');
    }
    console.error('Could not load landfalls:', error);
  } finally {
    stormCount?.classList.remove('value-loading');
    landfallCount?.classList.remove('value-loading');
    button?.removeAttribute('disabled');
    button?.classList.remove('is-loading');
    setButtonLabel('Load Florida Landfalls');
  }


});
