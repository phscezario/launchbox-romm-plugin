# API Integration

This document describes how the plugin integrates with the RomM REST API.

## Overview

The plugin communicates with a self-hosted RomM server via its REST API. The API client is implemented in `RommPlugin/ApiClient/RommApiClient.cs` with the interface `IRommApiClient`.

## Authentication

### Client API Token (Preferred)

Generate a token in RomM under **Administration → Client API Tokens**.

- Format: `rmm_` + 64 hex characters
- Sent as: `Authorization: Bearer rmm_...`
- Takes priority over username/password

### Username/Password

- Sent as: `Authorization: Basic base64(username:password)`
- Password is encrypted at rest using DPAPI

The `AuthHeaderHelper` (`RommPlugin.Core/Helpers/AuthHeaderHelper.cs`) constructs the appropriate header based on which credentials are configured.

## Base URL

All API calls are made to the configured `RommBaseUrl`:

```
{RommBaseUrl}/api/{endpoint}
```

Example: `http://192.168.1.100:9000/api/platforms`

## API Endpoints Used

### Platforms

| Method | Endpoint | Purpose |
|--------|----------|---------|
| GET | `/api/platforms` | List all platforms |

### Games (ROMs)

| Method | Endpoint | Purpose |
|--------|----------|---------|
| GET | `/api/roms` | List games (paginated) |
| GET | `/api/roms?id={id}` | Get single game details |
| PUT | `/api/roms/{id}` | Update game metadata |
| DELETE | `/api/roms/{id}` | Delete game |
| GET | `/api/roms/download/{id}` | Download game file |

### Screenshots

| Method | Endpoint | Purpose |
|--------|----------|---------|
| POST | `/api/screenshots/upload` | Upload screenshot |
| GET | `/api/screenshots` | List screenshots |

### Play Sessions

| Method | Endpoint | Purpose |
|--------|----------|---------|
| POST | `/api/play sessions` | Submit play session |
| GET | `/api/play sessions` | List play sessions |

### Assets

| Method | Endpoint | Purpose |
|--------|----------|---------|
| POST | `/api/assets/upload` | Upload cover art or other assets |

## Pagination

List endpoints support pagination:

```
GET /api/roms?page=1&per_page=1000
```

- `page`: Page number (1-based)
- `per_page`: Items per page (max: 1000, defined by `ApiPageSize` constant)

The client handles pagination automatically, fetching all pages until all results are retrieved.

## Data Models

### RomM Game (`RommGame`)

Key fields from the RomM API:

| Field | Type | Description |
|-------|------|-------------|
| `id` | int | RomM game ID |
| `name` | string | Game title |
| `slug` | string | URL-friendly name |
| `platform_id` | int | Parent platform ID |
| `files` | List | ROM files |
| `metadata` | object | Metadata from various sources |
| `screenshots` | List | Screenshot URLs |
| `cover` | string | Cover art URL |

### Metadata Sources

The RomM API returns metadata from multiple sources with configurable priority:

1. **LaunchBoxMetadata** - From LaunchBox database
2. **ScreenScraper** - From ScreenScraper API
3. **IGDB** - From IGDB API
4. **RomM Metadata** - Basic metadata from RomM

## Request/Response Flow

### Sync Flow

```
1. GET /api/platforms → List of platforms
2. For each selected platform:
   GET /api/roms?platform_id={id}&page=1&per_page=1000
   GET /api/roms?platform_id={id}&page=2&per_page=1000
   ... (until all games fetched)
3. For each game with changes:
   PUT /api/roms/{id} (if admin mode, push metadata)
   POST /api/assets/upload (if pushing cover art)
   POST /api/screenshots/upload (if pushing screenshots)
```

### Install Flow

```
1. GET /api/roms/download/{id} → Download game file
2. Extract ZIP if compressed
3. Fix nested folder structure
4. Configure executable paths
5. Mark as installed
```

## Error Handling

### Connection Errors

The `RommConnectionTester` (`RommPlugin.Core/Services/RommConnectionTester.cs`) validates connectivity before sync:

1. Tests HTTP connectivity to the server
2. Validates authentication credentials
3. Returns detailed error information

### HTTP Errors

| Status Code | Handling |
|-------------|----------|
| 401 | Authentication failed - prompt for credentials |
| 403 | Insufficient permissions |
| 404 | Resource not found - skip |
| 408 | Request timeout - retry |
| 429 | Rate limited - wait and retry |
| 500+ | Server error - retry with backoff |

### Retry Logic

- **Max retries:** 5 (configurable via `MaxRetryAttempts`)
- **Base delay:** 1000ms (configurable via `RetryBaseDelayMs`)
- **Backoff:** Exponential (1s, 2s, 4s, 8s, 16s)

### Download Resumption

Downloads support HTTP Range headers for resumption:

```
GET /api/roms/download/{id}
Range: bytes={offset}-
```

The `DownloadQueueService` tracks download progress and can resume interrupted downloads.

## Timeouts

| Operation | Timeout | Constant |
|-----------|---------|----------|
| HTTP requests | 120s | `HttpTimeoutSeconds` |
| File uploads | 300s | `UploadTimeoutSeconds` |
| HTTP buffer | 8192 bytes | `HttpBufferSize` |

## Connection Testing

The Settings UI includes a **Test Connection** button that:

1. Validates the server URL format
2. Attempts to connect to the server
3. Validates authentication credentials
4. Returns success/failure with error details

This uses `RommConnectionTester` (`RommPlugin.Core/Services/RommConnectionTester.cs`).

## Custom Fields

The plugin stores metadata in LaunchBox's custom fields (prefixed with `romm_`):

| Field | Description |
|-------|-------------|
| `romm_game_id` | RomM game ID |
| `romm_platform_id` | RomM platform ID |
| `romm_remote_path` | ROM file path on server |
| `romm_file_name` | ROM file name |
| `romm_isFolder_game` | Whether game is folder-based |
| `romm_last_synced_at` | Last sync timestamp |
| `romm_local_metadata_hash` | Hash of local metadata |
| `romm_remote_metadata_hash` | Hash of remote metadata |
| `romm_igdb_rating` | IGDB rating |
| `romm_ss_score` | ScreenScraper score |
| `romm_franchises` | Game franchises |
| `romm_game_modes` | Game modes |
| `romm_age_ratings` | Age ratings |
| `romm_player_count` | Player count |
| `romm_average_rating` | Average rating |

See `GameCustomFields.cs` for the complete list.
