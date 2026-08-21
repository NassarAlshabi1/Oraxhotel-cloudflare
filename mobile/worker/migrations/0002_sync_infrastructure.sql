-- Migration 0002: Worker infrastructure and desktop-first command/event ledger.
-- The desktop SQL Server remains the source of truth. Mobile writes become commands;
-- canonical desktop publications are recorded as events and projected for Flutter.

CREATE TABLE IF NOT EXISTS users (
  id TEXT PRIMARY KEY,
  username TEXT NOT NULL UNIQUE,
  password_hash TEXT NOT NULL,
  role TEXT NOT NULL DEFAULT 'staff',
  device_id TEXT,
  created_at INTEGER NOT NULL,
  updated_at INTEGER NOT NULL,
  deleted_at INTEGER
);
CREATE INDEX IF NOT EXISTS idx_users_username ON users(username);
CREATE TABLE IF NOT EXISTS devices (
  id TEXT PRIMARY KEY,
  device_id TEXT NOT NULL UNIQUE,
  fcm_token TEXT,
  status TEXT NOT NULL DEFAULT 'active',
  device_name TEXT,
  platform TEXT,
  created_at INTEGER NOT NULL,
  updated_at INTEGER NOT NULL
);
CREATE INDEX IF NOT EXISTS idx_devices_status ON devices(status);
CREATE TABLE IF NOT EXISTS rate_limits (
  client_id TEXT NOT NULL,
  window_start INTEGER NOT NULL,
  count INTEGER NOT NULL DEFAULT 0,
  PRIMARY KEY (client_id, window_start)
);
CREATE TABLE IF NOT EXISTS sync_log (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  entity TEXT NOT NULL,
  entity_id TEXT NOT NULL,
  operation TEXT NOT NULL,
  version INTEGER NOT NULL,
  device_id TEXT,
  timestamp INTEGER NOT NULL,
  payload TEXT
);
CREATE INDEX IF NOT EXISTS idx_sync_log_entity ON sync_log(entity, entity_id);
CREATE INDEX IF NOT EXISTS idx_sync_log_timestamp ON sync_log(timestamp);
CREATE TABLE IF NOT EXISTS sync_conflicts (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  entity TEXT NOT NULL,
  entity_id TEXT NOT NULL,
  local_payload TEXT NOT NULL,
  remote_payload TEXT NOT NULL,
  local_vector_clock TEXT,
  remote_vector_clock TEXT,
  resolution TEXT NOT NULL DEFAULT 'last_write_wins',
  resolved_at INTEGER,
  created_at INTEGER NOT NULL,
  device_id TEXT
);
CREATE INDEX IF NOT EXISTS idx_conflicts_entity ON sync_conflicts(entity, entity_id);
CREATE INDEX IF NOT EXISTS idx_conflicts_created ON sync_conflicts(created_at);
CREATE TABLE IF NOT EXISTS idempotency_log (
  key TEXT PRIMARY KEY,
  entity TEXT NOT NULL,
  operation TEXT NOT NULL,
  entity_id TEXT,
  processed_at INTEGER NOT NULL,
  response TEXT
);
CREATE INDEX IF NOT EXISTS idx_idempotency_entity ON idempotency_log(entity, entity_id);

CREATE TABLE IF NOT EXISTS "desktop_sync_events" (
  "event_id" TEXT PRIMARY KEY,
  "source_system" TEXT NOT NULL,
  "source_device" TEXT NOT NULL,
  "entity_table" TEXT NOT NULL,
  "entity_key" TEXT NOT NULL,
  "operation" TEXT NOT NULL,
  "occurred_at" TEXT NOT NULL,
  "actor_id" TEXT,
  "payload_json" TEXT NOT NULL,
  "idempotency_key" TEXT NOT NULL UNIQUE,
  "status" TEXT NOT NULL DEFAULT 'published',
  "created_at" INTEGER NOT NULL,
  "applied_at" INTEGER
);
CREATE INDEX IF NOT EXISTS "idx_desktop_events_entity" ON "desktop_sync_events" ("entity_table", "entity_key");
CREATE INDEX IF NOT EXISTS "idx_desktop_events_occurred" ON "desktop_sync_events" ("occurred_at");

CREATE TABLE IF NOT EXISTS "desktop_sync_commands" (
  "command_id" TEXT PRIMARY KEY,
  "idempotency_key" TEXT NOT NULL UNIQUE,
  "entity" TEXT NOT NULL,
  "operation" TEXT NOT NULL,
  "local_uuid" TEXT NOT NULL,
  "payload_json" TEXT NOT NULL,
  "vector_clock" TEXT NOT NULL DEFAULT '{}',
  "requested_at" INTEGER NOT NULL,
  "requested_by" TEXT,
  "status" TEXT NOT NULL DEFAULT 'pending',
  "processed_at" INTEGER,
  "result_json" TEXT,
  "error" TEXT
);
CREATE INDEX IF NOT EXISTS "idx_desktop_commands_status" ON "desktop_sync_commands" ("status", "requested_at");
CREATE INDEX IF NOT EXISTS "idx_desktop_commands_entity" ON "desktop_sync_commands" ("entity", "local_uuid");

CREATE TABLE IF NOT EXISTS "desktop_sync_checkpoints" (
  "source_system" TEXT NOT NULL,
  "entity_table" TEXT NOT NULL,
  "checkpoint_value" TEXT NOT NULL,
  "updated_at" INTEGER NOT NULL,
  PRIMARY KEY ("source_system", "entity_table")
);
