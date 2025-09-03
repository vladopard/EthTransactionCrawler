import './style.css';

const API = (import.meta as any).env?.VITE_API_BASE ?? 'https://localhost:7237';

const form = document.getElementById('query-form') as HTMLFormElement;
const addressInput = document.getElementById('address') as HTMLInputElement;
const startBlockInput = document.getElementById('startBlock') as HTMLInputElement;

const txTbody = document.querySelector('#tx-table tbody') as HTMLTableSectionElement;
const itxTbody = document.querySelector('#internal-table tbody') as HTMLTableSectionElement;
const tokTbody = document.querySelector('#tokens-table tbody') as HTMLTableSectionElement;

const loadMoreExtBtn = document.getElementById('load-more-external') as HTMLButtonElement;
const loadMoreIntBtn = document.getElementById('load-more-internal') as HTMLButtonElement;
const loadMoreTokBtn = document.getElementById('load-more-tokens') as HTMLButtonElement;

let currentAddress = '';
let fromBlock = 0;
let pageExt = 1, pageInt = 1, pageTok = 1;
const pageSize = 10;

function toUtc(ts: string) {
  return new Date(ts).toISOString().replace('T', ' ').replace('Z', ' UTC');
}

function dirOf(addr: string, from?: string) {
  return (from ?? '').toLowerCase() === addr ? 'out' : 'in';
}

// ---- ROW RENDERERS ----
function rowHtmlExternal(tx: any, addrLc: string) {
  const status = tx.isError ? 'failed' : 'success';
  return `<tr>
    <td>${toUtc(tx.timeStampUtc)}</td>
    <td>${tx.blockNumber}</td>
    <td><code>${tx.hash}</code></td>
    <td><code>${tx.from ?? ''}</code></td>
    <td><code>${tx.to ?? ''}</code></td>
    <td>${dirOf(addrLc, tx.from)}</td>
    <td>${tx.valueEth}</td>
    <td>${tx.gasUsed}</td>
    <td>${tx.gasPriceGwei ?? ''}</td>
    <td>${status}</td>
  </tr>`;
}

function rowHtmlInternal(itx: any, addrLc: string) {
  return `<tr>
    <td>${toUtc(itx.timeStampUtc)}</td>
    <td>${itx.blockNumber}</td>
    <td><code>${itx.uniqueId}</code></td>
    <td><code>${itx.hash}</code></td>
    <td><code>${itx.from ?? ''}</code></td>
    <td><code>${itx.to ?? ''}</code></td>
    <td>${dirOf(addrLc, itx.from)}</td>
    <td>${itx.valueEth}</td>
  </tr>`;
}

function rowHtmlToken(tt: any, addrLc: string) {
  return `<tr>
    <td>${toUtc(tt.timeStampUtc)}</td>
    <td>${tt.blockNumber}</td>
    <td><code>${tt.uniqueId}</code></td>
    <td><code>${tt.txHash}</code></td>
    <td><code>${tt.contractAddress}</code></td>
    <td>${tt.tokenSymbol}</td>
    <td>${tt.tokenDecimals}</td>
    <td><code>${tt.from ?? ''}</code></td>
    <td><code>${tt.to ?? ''}</code></td>
    <td>${dirOf(addrLc, tt.from)}</td>
    <td>${tt.amount}</td>
  </tr>`;
}

// ---- FETCH HELPERS ----
async function getJsonOrThrow(res: Response) {
  const ct = res.headers.get('content-type') || '';
  if (!ct.includes('application/json')) {
    const text = await res.text();
    throw new Error(`Expected JSON, got ${ct}. Status ${res.status}. Body:\n${text.slice(0,300)}...`);
  }
  return res.json();
}

async function loadExternalPage() {
  loadMoreExtBtn.disabled = true;
  const base = `${API}/api/addresses/${currentAddress}`;
  const url = `${base}/transactions?fromBlock=${fromBlock}&page=${pageExt}&pageSize=${pageSize}&persist=false`;
  const res = await fetch(url);
  const data = await getJsonOrThrow(res); // { total, page, pageSize, items: [...] }
  const addrLc = currentAddress.toLowerCase();
  txTbody.insertAdjacentHTML('beforeend', data.items.map((x: any) => rowHtmlExternal(x, addrLc)).join(''));
  pageExt++;
  loadMoreExtBtn.disabled = (data.items.length < pageSize);
}

async function loadInternalPage() {
  loadMoreIntBtn.disabled = true;
  const base = `${API}/api/addresses/${currentAddress}`;
  const url = `${base}/internal-transactions?fromBlock=${fromBlock}&page=${pageInt}&pageSize=${pageSize}&persist=false`;
  const res = await fetch(url);
  const data = await getJsonOrThrow(res);
  const addrLc = currentAddress.toLowerCase();
  itxTbody.insertAdjacentHTML('beforeend', data.items.map((x: any) => rowHtmlInternal(x, addrLc)).join(''));
  pageInt++;
  loadMoreIntBtn.disabled = (data.items.length < pageSize);
}

async function loadTokensPage() {
  loadMoreTokBtn.disabled = true;
  const base = `${API}/api/addresses/${currentAddress}`;
  const url = `${base}/token-transfers?fromBlock=${fromBlock}&page=${pageTok}&pageSize=${pageSize}&persist=false`;
  const res = await fetch(url);
  const data = await getJsonOrThrow(res);
  const addrLc = currentAddress.toLowerCase();
  tokTbody.insertAdjacentHTML('beforeend', data.items.map((x: any) => rowHtmlToken(x, addrLc)).join(''));
  pageTok++;
  loadMoreTokBtn.disabled = (data.items.length < pageSize);
}

// ---- EVENTS ----
form.addEventListener('submit', (e) => {
  e.preventDefault();
  currentAddress = addressInput.value.trim();
  fromBlock = Number(startBlockInput.value || '0');
  pageExt = pageInt = pageTok = 1;
  txTbody.innerHTML = '';
  itxTbody.innerHTML = '';
  tokTbody.innerHTML = '';
  // Učitaj prvu stranicu aktivnog taba
  const active = document.querySelector('.tabs button.active') as HTMLButtonElement;
  if (active?.dataset.tab === 'internal') loadInternalPage();
  else if (active?.dataset.tab === 'tokens') loadTokensPage();
  else loadExternalPage();
});

loadMoreExtBtn.addEventListener('click', loadExternalPage);
loadMoreIntBtn.addEventListener('click', loadInternalPage);
loadMoreTokBtn.addEventListener('click', loadTokensPage);

// tab switching
document.querySelectorAll('.tabs button').forEach(btn => {
  btn.addEventListener('click', () => {
    document.querySelectorAll('.tabs button').forEach(b => b.classList.remove('active'));
    btn.classList.add('active');
    document.querySelectorAll('.tab').forEach(s => s.classList.remove('active'));
    const tabName = (btn as HTMLButtonElement).dataset.tab;
    document.getElementById(`tab-${tabName}`)!.classList.add('active');
  });
});
