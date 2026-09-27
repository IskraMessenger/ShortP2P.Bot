-- Vendor-agnostic sketch of the messenger server pool table.
-- Adapt types/identity to the concrete RDBMS (SQL Server, PostgreSQL, SQLite, etc.).

CREATE TABLE messenger_servers (
    id                        BIGINT        NOT NULL PRIMARY KEY,
    base_url                  VARCHAR(512)  NOT NULL,
    network_id                VARCHAR(64)   NOT NULL,
    bot_key                   VARCHAR(128)  NOT NULL,
    is_available              BOOLEAN       NOT NULL DEFAULT TRUE,
    last_successful_at_utc    TIMESTAMP     NULL,
    registered_at_utc         TIMESTAMP     NOT NULL,
    last_health_check_at_utc  TIMESTAMP     NULL,
    last_error                VARCHAR(1024) NULL,
    CONSTRAINT uq_messenger_servers_base_url UNIQUE (base_url)
);

-- Suggested order for available pool polling:
-- ORDER BY last_successful_at_utc DESC NULLS LAST, registered_at_utc ASC, id ASC
