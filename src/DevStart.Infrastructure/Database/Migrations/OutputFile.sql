CREATE TABLE IF NOT EXISTS public."__EFMigrationsHistory" (
    migration_id character varying(150) NOT NULL,
    product_version character varying(32) NOT NULL,
    CONSTRAINT pk___ef_migrations_history PRIMARY KEY (migration_id)
);

START TRANSACTION;
CREATE TABLE public.email_verification_tokens (
    token_id uuid NOT NULL,
    user_id uuid NOT NULL,
    created_at timestamp with time zone NOT NULL,
    expires_at timestamp with time zone NOT NULL,
    CONSTRAINT pk_email_verification_tokens PRIMARY KEY (token_id)
);

CREATE TABLE public.media_files (
    id uuid NOT NULL,
    uploader_id uuid NOT NULL,
    object_name text NOT NULL,
    bucket text NOT NULL,
    file_type integer NOT NULL,
    file_size integer NOT NULL,
    upload_date timestamp with time zone NOT NULL,
    CONSTRAINT pk_media_files PRIMARY KEY (id)
);

CREATE TABLE public.profiles (
    user_id uuid NOT NULL,
    name text,
    bio text,
    url text,
    social_media_links jsonb NOT NULL,
    is_available_for_hire boolean NOT NULL,
    is_public boolean NOT NULL,
    avatar_url text,
    CONSTRAINT pk_profiles PRIMARY KEY (user_id)
);

CREATE TABLE public.startup_document_files (
    id uuid NOT NULL,
    name text NOT NULL,
    startup_id uuid NOT NULL,
    file_url text NOT NULL,
    file_type integer NOT NULL,
    upload_date timestamp with time zone NOT NULL,
    CONSTRAINT pk_startup_document_files PRIMARY KEY (id)
);

CREATE TABLE public.startup_followers (
    profile_id uuid NOT NULL,
    startup_id uuid NOT NULL,
    created_at timestamp with time zone NOT NULL,
    CONSTRAINT pk_startup_followers PRIMARY KEY (profile_id, startup_id)
);

CREATE TABLE public.startup_investors (
    profile_id uuid NOT NULL,
    startup_id uuid NOT NULL,
    is_public boolean NOT NULL,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    CONSTRAINT pk_startup_investors PRIMARY KEY (profile_id, startup_id)
);

CREATE TABLE public.startup_members (
    profile_id uuid NOT NULL,
    startup_id uuid NOT NULL,
    role integer NOT NULL,
    is_public boolean NOT NULL,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    CONSTRAINT pk_startup_members PRIMARY KEY (profile_id, startup_id)
);

CREATE TABLE public.startup_metrics (
    id uuid NOT NULL,
    startup_id uuid NOT NULL,
    metric_type integer NOT NULL,
    value numeric NOT NULL,
    created_at timestamp with time zone NOT NULL,
    CONSTRAINT pk_startup_metrics PRIMARY KEY (id)
);

CREATE TABLE public.startup_products (
    startup_id uuid NOT NULL,
    problem text NOT NULL,
    solution text NOT NULL,
    stack jsonb NOT NULL,
    value_proposition text NOT NULL,
    differentiators text NOT NULL,
    CONSTRAINT pk_startup_products PRIMARY KEY (startup_id)
);

CREATE TABLE public.startup_roadmap_items (
    id uuid NOT NULL,
    startup_id uuid NOT NULL,
    startup_stage integer NOT NULL,
    title text NOT NULL,
    description text,
    status integer NOT NULL,
    created_at timestamp with time zone NOT NULL,
    target_date timestamp with time zone NOT NULL,
    CONSTRAINT pk_startup_roadmap_items PRIMARY KEY (id)
);

CREATE TABLE public.startups (
    id uuid NOT NULL,
    name text NOT NULL,
    public_email text NOT NULL,
    description text NOT NULL,
    url text NOT NULL,
    is_stopped boolean NOT NULL,
    stage integer NOT NULL,
    social_media_links jsonb NOT NULL,
    location integer NOT NULL,
    billing_email text NOT NULL,
    avatar_url text NOT NULL,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    CONSTRAINT pk_startups PRIMARY KEY (id)
);

CREATE TABLE public.user_preferences (
    user_id uuid NOT NULL,
    theme integer NOT NULL,
    receive_notifications boolean NOT NULL,
    CONSTRAINT pk_user_preferences PRIMARY KEY (user_id)
);

CREATE TABLE public.users (
    id uuid NOT NULL,
    username character varying(100) NOT NULL,
    email character varying(255) NOT NULL,
    password_hash text NOT NULL,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NOT NULL,
    CONSTRAINT pk_users PRIMARY KEY (id)
);

CREATE INDEX ix_email_verification_tokens_user_id ON public.email_verification_tokens (user_id);

CREATE INDEX ix_startup_metrics_startup_id ON public.startup_metrics (startup_id);

CREATE INDEX ix_startup_roadmap_items_startup_id ON public.startup_roadmap_items (startup_id);

CREATE UNIQUE INDEX ix_users_email ON public.users (email);

CREATE UNIQUE INDEX ix_users_username ON public.users (username);

INSERT INTO public."__EFMigrationsHistory" (migration_id, product_version)
VALUES ('20260128110737_Create_Database', '10.0.1');

COMMIT;

