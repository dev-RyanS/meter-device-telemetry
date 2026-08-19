import { FormEvent, useEffect, useState } from 'react';

type Reading = {
  tenantId: string;
  deviceId: string;
  type: string;
  value: number;
  unit: string;
  battery: number;
  signal: number;
  recordedAt: string;
  externalId: string;
};

type ReadingsResponse = {
  items: Reading[];
  page: number;
  pageSize: number;
  totalCount: number;
};

const apiBaseUrl = import.meta.env.VITE_API_BASE_URL || '/api';

function App() {
  const [tenantId, setTenantId] = useState('acme');
  const [deviceId, setDeviceId] = useState('');
  const [type, setType] = useState('');
  const [readings, setReadings] = useState<Reading[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function loadReadings() {
    if (!tenantId.trim()) {
      setError('Enter a tenant ID to load readings.');
      return;
    }

    setIsLoading(true);
    setError(null);

    const parameters = new URLSearchParams({
      tenantId: tenantId.trim(),
      page: '1',
      pageSize: '20'
    });

    if (deviceId.trim()) {
      parameters.set('deviceId', deviceId.trim());
    }

    if (type.trim()) {
      parameters.set('type', type.trim());
    }

    try {
      const response = await fetch(`${apiBaseUrl}/readings?${parameters}`);

      if (!response.ok) {
        throw new Error(`The API returned HTTP ${response.status}.`);
      }

      const result = (await response.json()) as ReadingsResponse;
      setReadings(result.items);
      setTotalCount(result.totalCount);
    } catch (requestError) {
      const message = requestError instanceof Error
        ? requestError.message
        : 'The readings could not be loaded.';
      setError(message);
      setReadings([]);
      setTotalCount(0);
    } finally {
      setIsLoading(false);
    }
  }

  useEffect(() => {
    void loadReadings();
  }, []);

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    void loadReadings();
  }

  return (
    <main className="page-shell">
      <header className="page-header">
        <div>
          <p className="eyebrow">Telemetry console</p>
          <h1>Recent meter readings</h1>
          <p className="subtitle">A quick view of the latest readings received by the service.</p>
        </div>
        <div className="status-mark" aria-label="Read-only view">LIVE VIEW</div>
      </header>

      <section className="filter-panel" aria-label="Reading filters">
        <form onSubmit={handleSubmit}>
          <label>
            Tenant
            <input
              value={tenantId}
              onChange={(event) => setTenantId(event.target.value)}
              placeholder="acme"
              required
            />
          </label>
          <label>
            Device
            <input
              value={deviceId}
              onChange={(event) => setDeviceId(event.target.value)}
              placeholder="Any device"
            />
          </label>
          <label>
            Type
            <input
              value={type}
              onChange={(event) => setType(event.target.value)}
              placeholder="Any type"
            />
          </label>
          <button type="submit">Refresh readings</button>
        </form>
      </section>

      <section className="results-section" aria-live="polite">
        <div className="results-heading">
          <div>
            <p className="eyebrow">Latest window</p>
            <h2>{totalCount} readings found</h2>
          </div>
          <span className="sort-note">Newest first</span>
        </div>

        {isLoading && <p className="message">Loading readings...</p>}
        {error && <p className="message error-message">{error}</p>}

        {!isLoading && !error && readings.length === 0 && (
          <p className="message">No readings match these filters.</p>
        )}

        {!isLoading && !error && readings.length > 0 && (
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>Recorded</th>
                  <th>Device</th>
                  <th>Type</th>
                  <th>Value</th>
                  <th>Battery</th>
                  <th>Signal</th>
                  <th>External ID</th>
                </tr>
              </thead>
              <tbody>
                {readings.map((reading) => (
                  <tr key={`${reading.tenantId}-${reading.externalId}`}>
                    <td>{new Date(reading.recordedAt).toLocaleString()}</td>
                    <td className="strong-cell">{reading.deviceId}</td>
                    <td><span className="type-tag">{reading.type}</span></td>
                    <td>{reading.value} {reading.unit}</td>
                    <td>{reading.battery}%</td>
                    <td>{reading.signal} dBm</td>
                    <td className="muted-cell">{reading.externalId}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>
    </main>
  );
}

export default App;