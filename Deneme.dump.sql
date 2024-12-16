--
-- PostgreSQL database dump
--

-- Dumped from database version 16.6
-- Dumped by pg_dump version 16.6

-- Started on 2024-12-16 22:22:16

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

--
-- TOC entry 231 (class 1255 OID 16829)
-- Name: hesapla_gunluk_gelir(date); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.hesapla_gunluk_gelir(tarih date) RETURNS numeric
    LANGUAGE plpgsql
    AS $$
DECLARE
    toplam_gelir NUMERIC(10, 2);
BEGIN
    -- Verilen tarih için toplam gelir hesaplanır
    SELECT COALESCE(SUM(y.islem_ucreti), 0) INTO toplam_gelir
    FROM randevular r
    JOIN yapilabilen_islemler y ON r.islem_id = y.yapilabilen_islemler_id
    WHERE r.gun = tarih
      AND r.onaylandi = TRUE; -- Sadece onaylanmış randevular dikkate alınır

    -- Toplam geliri döndür
    RETURN toplam_gelir;
END;
$$;


ALTER FUNCTION public.hesapla_gunluk_gelir(tarih date) OWNER TO postgres;

--
-- TOC entry 229 (class 1255 OID 16825)
-- Name: kontrol_randevu_uygunlugu(); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.kontrol_randevu_uygunlugu() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
DECLARE
    islem_suresi TIME;         -- İşlem süresi
    randevu_bitis_saat TIME;   -- Randevunun bitiş saati
BEGIN
    -- 1. Aynı saatte, aynı salonda ve aynı personelde onaylanmış bir randevu var mı?
    IF EXISTS (
        SELECT 1
        FROM randevular
        WHERE salon_id = NEW.salon_id
          AND personel_id = NEW.personel_id
          AND gun = NEW.gun
          AND NEW.saat BETWEEN saat AND saat + (SELECT islem_suresi FROM yapilabilen_islemler WHERE yapilabilen_islemler_id = islem_id)
          AND onaylandi = TRUE
    ) THEN
        RAISE EXCEPTION 'Bu saatte onaylanmış başka bir randevu var!';
    END IF;

    -- 2. İlgili işlemin süresini bul
    SELECT islem_suresi INTO islem_suresi
    FROM yapilabilen_islemler
    WHERE yapilabilen_islemler_id = NEW.islem_id;

    -- İşlemin bitiş saatini hesapla
    randevu_bitis_saat := NEW.saat + islem_suresi;

    -- 3. Salon çalışma saatlerini kontrol et
    IF NOT EXISTS (
        SELECT 1
        FROM salonlar
        WHERE salon_id = NEW.salon_id
          AND NEW.saat >= baslangic_saat
          AND randevu_bitis_saat <= bitis_saat
    ) THEN
        RAISE EXCEPTION 'Randevu salonun çalışma saatleriyle uyumlu değil!';
    END IF;

    -- 4. Personel çalışma saatlerini kontrol et
    IF NOT EXISTS (
        SELECT 1
        FROM personel
        WHERE personel_id = NEW.personel_id
          AND NEW.saat >= baslangic_saat
          AND randevu_bitis_saat <= bitis_saat
    ) THEN
        RAISE EXCEPTION 'Randevu personelin çalışma saatleriyle uyumlu değil!';
    END IF;

    -- 5. Personelin uzmanlık alanı kontrolü
    IF NOT EXISTS (
        SELECT 1
        FROM yapilabilen_islemler yi
        WHERE yi.yapilabilen_islemler_id = NEW.islem_id
          AND yi.uzmanlik_alani_id = ANY (
              SELECT UNNEST(uzmanlik_alanlari) FROM personel WHERE personel_id = NEW.personel_id
          )
    ) THEN
        RAISE EXCEPTION 'Personelin bu işlemi yapma yetkinliği yok!';
    END IF;

    -- Tüm kontroller başarılı, randevu oluşturulabilir
    RETURN NEW;
END;
$$;


ALTER FUNCTION public.kontrol_randevu_uygunlugu() OWNER TO postgres;

--
-- TOC entry 230 (class 1255 OID 16827)
-- Name: sil_cakisan_randevular(); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.sil_cakisan_randevular() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
BEGIN
    -- Eğer randevu onaylandıysa
    IF NEW.onaylandi = TRUE THEN
        -- Çakışan tüm randevuları sil
        DELETE FROM randevular
        WHERE salon_id = NEW.salon_id
          AND personel_id = NEW.personel_id
          AND gun = NEW.gun
          AND randevu_id != NEW.randevu_id -- Kendisi hariç
          AND NEW.saat BETWEEN saat AND saat + (SELECT islem_suresi FROM yapilabilen_islemler WHERE yapilabilen_islemler_id = islem_id);
    END IF;

    -- Güncellenmiş randevuyu kaydet
    RETURN NEW;
END;
$$;


ALTER FUNCTION public.sil_cakisan_randevular() OWNER TO postgres;

SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- TOC entry 216 (class 1259 OID 16732)
-- Name: konumlar; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.konumlar (
    konum_id integer NOT NULL,
    konum_adi character varying(255) NOT NULL
);


ALTER TABLE public.konumlar OWNER TO postgres;

--
-- TOC entry 215 (class 1259 OID 16731)
-- Name: konumlar_konum_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.konumlar_konum_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.konumlar_konum_id_seq OWNER TO postgres;

--
-- TOC entry 4918 (class 0 OID 0)
-- Dependencies: 215
-- Name: konumlar_konum_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.konumlar_konum_id_seq OWNED BY public.konumlar.konum_id;


--
-- TOC entry 224 (class 1259 OID 16770)
-- Name: personel; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.personel (
    personel_id integer NOT NULL,
    salon_id integer,
    uzmanlik_alanlari integer[] NOT NULL,
    baslangic_saat time without time zone NOT NULL,
    bitis_saat time without time zone NOT NULL
);


ALTER TABLE public.personel OWNER TO postgres;

--
-- TOC entry 223 (class 1259 OID 16769)
-- Name: personel_personel_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.personel_personel_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.personel_personel_id_seq OWNER TO postgres;

--
-- TOC entry 4919 (class 0 OID 0)
-- Dependencies: 223
-- Name: personel_personel_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.personel_personel_id_seq OWNED BY public.personel.personel_id;


--
-- TOC entry 228 (class 1259 OID 16797)
-- Name: randevular; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.randevular (
    randevu_id integer NOT NULL,
    salon_id integer,
    personel_id integer,
    uye_id integer,
    islem_id integer,
    gun date NOT NULL,
    saat time without time zone NOT NULL,
    onaylandi boolean DEFAULT false
);


ALTER TABLE public.randevular OWNER TO postgres;

--
-- TOC entry 227 (class 1259 OID 16796)
-- Name: randevular_randevu_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.randevular_randevu_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.randevular_randevu_id_seq OWNER TO postgres;

--
-- TOC entry 4920 (class 0 OID 0)
-- Dependencies: 227
-- Name: randevular_randevu_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.randevular_randevu_id_seq OWNED BY public.randevular.randevu_id;


--
-- TOC entry 218 (class 1259 OID 16739)
-- Name: salonlar; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.salonlar (
    salon_id integer NOT NULL,
    konum_id integer,
    baslangic_saat time without time zone NOT NULL,
    bitis_saat time without time zone NOT NULL
);


ALTER TABLE public.salonlar OWNER TO postgres;

--
-- TOC entry 217 (class 1259 OID 16738)
-- Name: salonlar_salon_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.salonlar_salon_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.salonlar_salon_id_seq OWNER TO postgres;

--
-- TOC entry 4921 (class 0 OID 0)
-- Dependencies: 217
-- Name: salonlar_salon_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.salonlar_salon_id_seq OWNED BY public.salonlar.salon_id;


--
-- TOC entry 226 (class 1259 OID 16784)
-- Name: uyeler; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.uyeler (
    uye_id integer NOT NULL,
    email character varying(255) NOT NULL,
    password character varying(255) NOT NULL,
    admin_mi boolean DEFAULT false,
    CONSTRAINT uyeler_email_check CHECK (((email)::text ~* '^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$'::text))
);


ALTER TABLE public.uyeler OWNER TO postgres;

--
-- TOC entry 225 (class 1259 OID 16783)
-- Name: uyeler_uye_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.uyeler_uye_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.uyeler_uye_id_seq OWNER TO postgres;

--
-- TOC entry 4922 (class 0 OID 0)
-- Dependencies: 225
-- Name: uyeler_uye_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.uyeler_uye_id_seq OWNED BY public.uyeler.uye_id;


--
-- TOC entry 220 (class 1259 OID 16751)
-- Name: uzmanlik_alanlari; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.uzmanlik_alanlari (
    uzmanlik_alani_id integer NOT NULL,
    uzmanlik_adi character varying(255) NOT NULL
);


ALTER TABLE public.uzmanlik_alanlari OWNER TO postgres;

--
-- TOC entry 219 (class 1259 OID 16750)
-- Name: uzmanlik_alanlari_uzmanlik_alani_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.uzmanlik_alanlari_uzmanlik_alani_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.uzmanlik_alanlari_uzmanlik_alani_id_seq OWNER TO postgres;

--
-- TOC entry 4923 (class 0 OID 0)
-- Dependencies: 219
-- Name: uzmanlik_alanlari_uzmanlik_alani_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.uzmanlik_alanlari_uzmanlik_alani_id_seq OWNED BY public.uzmanlik_alanlari.uzmanlik_alani_id;


--
-- TOC entry 222 (class 1259 OID 16758)
-- Name: yapilabilen_islemler; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.yapilabilen_islemler (
    yapilabilen_islemler_id integer NOT NULL,
    uzmanlik_alani_id integer,
    islem_adi character varying(255) NOT NULL,
    islem_ucreti numeric(10,2) NOT NULL,
    islem_suresi time without time zone NOT NULL
);


ALTER TABLE public.yapilabilen_islemler OWNER TO postgres;

--
-- TOC entry 221 (class 1259 OID 16757)
-- Name: yapilabilen_islemler_yapilabilen_islemler_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.yapilabilen_islemler_yapilabilen_islemler_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.yapilabilen_islemler_yapilabilen_islemler_id_seq OWNER TO postgres;

--
-- TOC entry 4924 (class 0 OID 0)
-- Dependencies: 221
-- Name: yapilabilen_islemler_yapilabilen_islemler_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.yapilabilen_islemler_yapilabilen_islemler_id_seq OWNED BY public.yapilabilen_islemler.yapilabilen_islemler_id;


--
-- TOC entry 4721 (class 2604 OID 16735)
-- Name: konumlar konum_id; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.konumlar ALTER COLUMN konum_id SET DEFAULT nextval('public.konumlar_konum_id_seq'::regclass);


--
-- TOC entry 4725 (class 2604 OID 16773)
-- Name: personel personel_id; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.personel ALTER COLUMN personel_id SET DEFAULT nextval('public.personel_personel_id_seq'::regclass);


--
-- TOC entry 4728 (class 2604 OID 16800)
-- Name: randevular randevu_id; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.randevular ALTER COLUMN randevu_id SET DEFAULT nextval('public.randevular_randevu_id_seq'::regclass);


--
-- TOC entry 4722 (class 2604 OID 16742)
-- Name: salonlar salon_id; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.salonlar ALTER COLUMN salon_id SET DEFAULT nextval('public.salonlar_salon_id_seq'::regclass);


--
-- TOC entry 4726 (class 2604 OID 16787)
-- Name: uyeler uye_id; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.uyeler ALTER COLUMN uye_id SET DEFAULT nextval('public.uyeler_uye_id_seq'::regclass);


--
-- TOC entry 4723 (class 2604 OID 16754)
-- Name: uzmanlik_alanlari uzmanlik_alani_id; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.uzmanlik_alanlari ALTER COLUMN uzmanlik_alani_id SET DEFAULT nextval('public.uzmanlik_alanlari_uzmanlik_alani_id_seq'::regclass);


--
-- TOC entry 4724 (class 2604 OID 16761)
-- Name: yapilabilen_islemler yapilabilen_islemler_id; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.yapilabilen_islemler ALTER COLUMN yapilabilen_islemler_id SET DEFAULT nextval('public.yapilabilen_islemler_yapilabilen_islemler_id_seq'::regclass);


--
-- TOC entry 4900 (class 0 OID 16732)
-- Dependencies: 216
-- Data for Name: konumlar; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.konumlar (konum_id, konum_adi) FROM stdin;
\.


--
-- TOC entry 4908 (class 0 OID 16770)
-- Dependencies: 224
-- Data for Name: personel; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.personel (personel_id, salon_id, uzmanlik_alanlari, baslangic_saat, bitis_saat) FROM stdin;
\.


--
-- TOC entry 4912 (class 0 OID 16797)
-- Dependencies: 228
-- Data for Name: randevular; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.randevular (randevu_id, salon_id, personel_id, uye_id, islem_id, gun, saat, onaylandi) FROM stdin;
\.


--
-- TOC entry 4902 (class 0 OID 16739)
-- Dependencies: 218
-- Data for Name: salonlar; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.salonlar (salon_id, konum_id, baslangic_saat, bitis_saat) FROM stdin;
\.


--
-- TOC entry 4910 (class 0 OID 16784)
-- Dependencies: 226
-- Data for Name: uyeler; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.uyeler (uye_id, email, password, admin_mi) FROM stdin;
\.


--
-- TOC entry 4904 (class 0 OID 16751)
-- Dependencies: 220
-- Data for Name: uzmanlik_alanlari; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.uzmanlik_alanlari (uzmanlik_alani_id, uzmanlik_adi) FROM stdin;
\.


--
-- TOC entry 4906 (class 0 OID 16758)
-- Dependencies: 222
-- Data for Name: yapilabilen_islemler; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.yapilabilen_islemler (yapilabilen_islemler_id, uzmanlik_alani_id, islem_adi, islem_ucreti, islem_suresi) FROM stdin;
\.


--
-- TOC entry 4925 (class 0 OID 0)
-- Dependencies: 215
-- Name: konumlar_konum_id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.konumlar_konum_id_seq', 1, false);


--
-- TOC entry 4926 (class 0 OID 0)
-- Dependencies: 223
-- Name: personel_personel_id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.personel_personel_id_seq', 1, false);


--
-- TOC entry 4927 (class 0 OID 0)
-- Dependencies: 227
-- Name: randevular_randevu_id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.randevular_randevu_id_seq', 1, false);


--
-- TOC entry 4928 (class 0 OID 0)
-- Dependencies: 217
-- Name: salonlar_salon_id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.salonlar_salon_id_seq', 1, false);


--
-- TOC entry 4929 (class 0 OID 0)
-- Dependencies: 225
-- Name: uyeler_uye_id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.uyeler_uye_id_seq', 1, false);


--
-- TOC entry 4930 (class 0 OID 0)
-- Dependencies: 219
-- Name: uzmanlik_alanlari_uzmanlik_alani_id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.uzmanlik_alanlari_uzmanlik_alani_id_seq', 1, false);


--
-- TOC entry 4931 (class 0 OID 0)
-- Dependencies: 221
-- Name: yapilabilen_islemler_yapilabilen_islemler_id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.yapilabilen_islemler_yapilabilen_islemler_id_seq', 1, false);


--
-- TOC entry 4732 (class 2606 OID 16737)
-- Name: konumlar konumlar_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.konumlar
    ADD CONSTRAINT konumlar_pkey PRIMARY KEY (konum_id);


--
-- TOC entry 4740 (class 2606 OID 16777)
-- Name: personel personel_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.personel
    ADD CONSTRAINT personel_pkey PRIMARY KEY (personel_id);


--
-- TOC entry 4746 (class 2606 OID 16803)
-- Name: randevular randevular_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.randevular
    ADD CONSTRAINT randevular_pkey PRIMARY KEY (randevu_id);


--
-- TOC entry 4734 (class 2606 OID 16744)
-- Name: salonlar salonlar_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.salonlar
    ADD CONSTRAINT salonlar_pkey PRIMARY KEY (salon_id);


--
-- TOC entry 4742 (class 2606 OID 16795)
-- Name: uyeler uyeler_email_key; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.uyeler
    ADD CONSTRAINT uyeler_email_key UNIQUE (email);


--
-- TOC entry 4744 (class 2606 OID 16793)
-- Name: uyeler uyeler_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.uyeler
    ADD CONSTRAINT uyeler_pkey PRIMARY KEY (uye_id);


--
-- TOC entry 4736 (class 2606 OID 16756)
-- Name: uzmanlik_alanlari uzmanlik_alanlari_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.uzmanlik_alanlari
    ADD CONSTRAINT uzmanlik_alanlari_pkey PRIMARY KEY (uzmanlik_alani_id);


--
-- TOC entry 4738 (class 2606 OID 16763)
-- Name: yapilabilen_islemler yapilabilen_islemler_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.yapilabilen_islemler
    ADD CONSTRAINT yapilabilen_islemler_pkey PRIMARY KEY (yapilabilen_islemler_id);


--
-- TOC entry 4754 (class 2620 OID 16826)
-- Name: randevular randevu_uygunluk_kontrolu; Type: TRIGGER; Schema: public; Owner: postgres
--

CREATE TRIGGER randevu_uygunluk_kontrolu BEFORE INSERT ON public.randevular FOR EACH ROW EXECUTE FUNCTION public.kontrol_randevu_uygunlugu();


--
-- TOC entry 4755 (class 2620 OID 16828)
-- Name: randevular sil_cakisan_randevular_trigger; Type: TRIGGER; Schema: public; Owner: postgres
--

CREATE TRIGGER sil_cakisan_randevular_trigger AFTER UPDATE ON public.randevular FOR EACH ROW WHEN ((old.onaylandi IS DISTINCT FROM new.onaylandi)) EXECUTE FUNCTION public.sil_cakisan_randevular();


--
-- TOC entry 4749 (class 2606 OID 16778)
-- Name: personel personel_salon_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.personel
    ADD CONSTRAINT personel_salon_id_fkey FOREIGN KEY (salon_id) REFERENCES public.salonlar(salon_id) ON DELETE CASCADE;


--
-- TOC entry 4750 (class 2606 OID 16819)
-- Name: randevular randevular_islem_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.randevular
    ADD CONSTRAINT randevular_islem_id_fkey FOREIGN KEY (islem_id) REFERENCES public.yapilabilen_islemler(yapilabilen_islemler_id) ON DELETE CASCADE;


--
-- TOC entry 4751 (class 2606 OID 16809)
-- Name: randevular randevular_personel_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.randevular
    ADD CONSTRAINT randevular_personel_id_fkey FOREIGN KEY (personel_id) REFERENCES public.personel(personel_id) ON DELETE CASCADE;


--
-- TOC entry 4752 (class 2606 OID 16804)
-- Name: randevular randevular_salon_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.randevular
    ADD CONSTRAINT randevular_salon_id_fkey FOREIGN KEY (salon_id) REFERENCES public.salonlar(salon_id) ON DELETE CASCADE;


--
-- TOC entry 4753 (class 2606 OID 16814)
-- Name: randevular randevular_uye_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.randevular
    ADD CONSTRAINT randevular_uye_id_fkey FOREIGN KEY (uye_id) REFERENCES public.uyeler(uye_id) ON DELETE CASCADE;


--
-- TOC entry 4747 (class 2606 OID 16745)
-- Name: salonlar salonlar_konum_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.salonlar
    ADD CONSTRAINT salonlar_konum_id_fkey FOREIGN KEY (konum_id) REFERENCES public.konumlar(konum_id) ON DELETE CASCADE;


--
-- TOC entry 4748 (class 2606 OID 16764)
-- Name: yapilabilen_islemler yapilabilen_islemler_uzmanlik_alani_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.yapilabilen_islemler
    ADD CONSTRAINT yapilabilen_islemler_uzmanlik_alani_id_fkey FOREIGN KEY (uzmanlik_alani_id) REFERENCES public.uzmanlik_alanlari(uzmanlik_alani_id) ON DELETE CASCADE;


-- Completed on 2024-12-16 22:22:16

--
-- PostgreSQL database dump complete
--

