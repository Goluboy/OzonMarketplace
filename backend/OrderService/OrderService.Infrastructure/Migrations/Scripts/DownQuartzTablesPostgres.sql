-- This script drops all Quartz.NET tables and indexes for PostgreSQL
-- Used in FluentMigrator Down() migration

-- Suppress notices for cleaner output
SET client_min_messages = WARNING;

-- Drop indexes first (they will be dropped with tables, but explicit is better)
DROP INDEX IF EXISTS idx_qrtz_j_req_recovery;
DROP INDEX IF EXISTS idx_qrtz_t_next_fire_time;
DROP INDEX IF EXISTS idx_qrtz_t_state;
DROP INDEX IF EXISTS idx_qrtz_t_nft_st;
DROP INDEX IF EXISTS idx_qrtz_ft_trig_name;
DROP INDEX IF EXISTS idx_qrtz_ft_trig_group;
DROP INDEX IF EXISTS idx_qrtz_ft_trig_nm_gp;
DROP INDEX IF EXISTS idx_qrtz_ft_trig_inst_name;
DROP INDEX IF EXISTS idx_qrtz_ft_job_name;
DROP INDEX IF EXISTS idx_qrtz_ft_job_group;
DROP INDEX IF EXISTS idx_qrtz_ft_job_req_recovery;

-- Drop tables in dependency order (child tables first, then parents)
-- Tables with FK referencing qrtz_triggers
DROP TABLE IF EXISTS qrtz_simple_triggers CASCADE;
DROP TABLE IF EXISTS qrtz_simprop_triggers CASCADE;
DROP TABLE IF EXISTS qrtz_cron_triggers CASCADE;
DROP TABLE IF EXISTS qrtz_blob_triggers CASCADE;

-- qrtz_triggers has FK to qrtz_job_details
DROP TABLE IF EXISTS qrtz_triggers CASCADE;

-- Parent table
DROP TABLE IF EXISTS qrtz_job_details CASCADE;

-- Independent tables (no FK dependencies)
DROP TABLE IF EXISTS qrtz_calendars CASCADE;
DROP TABLE IF EXISTS qrtz_paused_trigger_grps CASCADE;
DROP TABLE IF EXISTS qrtz_fired_triggers CASCADE;
DROP TABLE IF EXISTS qrtz_scheduler_state CASCADE;
DROP TABLE IF EXISTS qrtz_locks CASCADE;

-- Restore normal message level
SET client_min_messages = NOTICE;