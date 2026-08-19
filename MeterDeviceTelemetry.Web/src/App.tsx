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

type ReadingForm = {
  tenantId: string;
  deviceId: string;
  type: string;
  value: string;
  unit: string;
  battery: string;
  signal: string;
  recordedAt: string;
  externalId: string;
};

type CreateReadingResponse = {
  status: {
    batteryLow: boolean;
    batteryThreshold: number;
  };
};

const apiBaseUrl = import.meta.env.VITE_API_BASE_URL || '/api';

const emptyReadingForm: ReadingForm = {
  tenantId: 'acme',
  deviceId: 'dev-123',
  type: 'water_level',
  value: '1.23',
  unit: 'm',
  battery: '62',
  signal: '-85',
  recordedAt: '2025-01-10T10:15:00Z',
  externalId: ''
};

function App() {
  const [tenantId, setTenantId] = useState('acme');
  const [deviceId, setDeviceId] = useState('');
  const [type, setType] = useState('');
  const [readings, setReadings] = useState<Reading[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const [readingForm, setReadingForm] = useState(emptyReadingForm);

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

  async function submitReading(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setIsLoading(true);
    setError(null);
    setSuccessMessage(null);

    try {
      const response = await fetch(`${apiBaseUrl}/readings`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          ...readingForm,
          value: Number(readingForm.value),
          battery: Number(readingForm.battery),
          signal: Number(readingForm.signal)
        })
      });

      const body = await response.json() as CreateReadingResponse & { errors?: string[] };

      if (!response.ok) {
        throw new Error(body.errors?.join(' ') || `The API returned HTTP ${response.status}.`);
      }

      const batteryStatus = body.status.batteryLow
        ? 'Battery is low.'
        : 'Battery is healthy.';
      setSuccessMessage(`Reading stored. ${batteryStatus}`);
      setReadingForm(emptyReadingForm);
      await loadReadings();
    } catch (requestError) {
      const message = requestError instanceof Error
        ? requestError.message
        : 'The reading could not be submitted.';
      setError(message);
    } finally {
      setIsLoading(false);
    }
  }

  function updateReadingForm(field: keyof ReadingForm, value: string) {
    setReadingForm((current) => ({ ...current, [field]: value }));
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

      <section className="submit-panel" aria-label="Submit a reading">
        <div className="panel-heading">
          <div>
            <p className="eyebrow">Single ingest</p>
            <h2>Submit a reading</h2>
          </div>
          <span className="sort-note">POST /api/readings</span>
        </div>
        <form className="reading-form" onSubmit={submitReading}>
          <label>
            Tenant
            <input value={readingForm.tenantId} onChange={(event) => updateReadingForm('tenantId', event.target.value)} required />
          </label>
          <label>
            Device
            <input value={readingForm.deviceId} onChange={(event) => updateReadingForm('deviceId', event.target.value)} required />
          </label>
          <label>
            Type
            <input value={readingForm.type} onChange={(event) => updateReadingForm('type', event.target.value)} required />
          </label>
          <label>
            Value
            <input type="number" step="any" value={readingForm.value} onChange={(event) => updateReadingForm('value', event.target.value)} required />
          </label>
          <label>
            Unit
            <input value={readingForm.unit} onChange={(event) => updateReadingForm('unit', event.target.value)} required />
          </label>
          <label>
            Battery %
            <input type="number" min="0" max="100" value={readingForm.battery} onChange={(event) => updateReadingForm('battery', event.target.value)} required />
          </label>
          <label>
            Signal dBm
            <input type="number" min="-150" max="0" value={readingForm.signal} onChange={(event) => updateReadingForm('signal', event.target.value)} required />
          </label>
          <label>
            Recorded at (UTC)
            <input value={readingForm.recordedAt} onChange={(event) => updateReadingForm('recordedAt', event.target.value)} required />
          </label>
          <label>
            External ID
            <input value={readingForm.externalId} onChange={(event) => updateReadingForm('externalId', event.target.value)} placeholder="r-789" required />
          </label>
          <button type="submit" disabled={isLoading}>Submit reading</button>
        </form>
      </section>

      {successMessage && <p className="message success-message">{successMessage}</p>}

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