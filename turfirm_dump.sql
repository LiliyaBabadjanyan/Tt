--
-- PostgreSQL database dump
--

-- Dumped from database version 17.4
-- Dumped by pg_dump version 17.4

-- Started on 2025-05-02 00:38:22

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET transaction_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

--
-- TOC entry 228 (class 1255 OID 24631)
-- Name: assign_putevka_on_new_tourist(); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.assign_putevka_on_new_tourist() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
DECLARE
    selected_season_id INT;
BEGIN
    -- Находим ID сезона с минимальной ценой из таблицы tours
    SELECT s.season_id
    INTO selected_season_id
    FROM public.seasons s
    JOIN public.tours t ON s.tour_id = t.tour_id
    ORDER BY t.price ASC
    LIMIT 1;

    -- Если найден — вставляем путевку
    IF selected_season_id IS NOT NULL THEN
        INSERT INTO public.putevki (tourist_id, season_id)
        VALUES (NEW.tourist_id, selected_season_id);
    END IF;

    RETURN NEW;
END;
$$;


ALTER FUNCTION public.assign_putevka_on_new_tourist() OWNER TO postgres;

SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- TOC entry 225 (class 1259 OID 24619)
-- Name: payment; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.payment (
    payment_id integer NOT NULL,
    putevki_id integer NOT NULL,
    payment_date date NOT NULL,
    summa money NOT NULL
);


ALTER TABLE public.payment OWNER TO postgres;

--
-- TOC entry 224 (class 1259 OID 24618)
-- Name: payment_payment_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

ALTER TABLE public.payment ALTER COLUMN payment_id ADD GENERATED ALWAYS AS IDENTITY (
    SEQUENCE NAME public.payment_payment_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- TOC entry 223 (class 1259 OID 24603)
-- Name: putevki; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.putevki (
    putevki_id integer NOT NULL,
    tourist_id integer NOT NULL,
    season_id integer NOT NULL
);


ALTER TABLE public.putevki OWNER TO postgres;

--
-- TOC entry 222 (class 1259 OID 24602)
-- Name: putevki_putevki_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

ALTER TABLE public.putevki ALTER COLUMN putevki_id ADD GENERATED ALWAYS AS IDENTITY (
    SEQUENCE NAME public.putevki_putevki_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- TOC entry 219 (class 1259 OID 24585)
-- Name: seasons; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.seasons (
    season_id integer NOT NULL,
    tour_id integer NOT NULL,
    start_date date NOT NULL,
    end_date date NOT NULL,
    closed boolean DEFAULT false,
    amount integer NOT NULL
);


ALTER TABLE public.seasons OWNER TO postgres;

--
-- TOC entry 218 (class 1259 OID 24584)
-- Name: seasons_season_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

ALTER TABLE public.seasons ALTER COLUMN season_id ADD GENERATED ALWAYS AS IDENTITY (
    SEQUENCE NAME public.seasons_season_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- TOC entry 227 (class 1259 OID 32834)
-- Name: selected_season_id; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.selected_season_id (
    season_id integer
);


ALTER TABLE public.selected_season_id OWNER TO postgres;

--
-- TOC entry 221 (class 1259 OID 24597)
-- Name: tourists; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.tourists (
    tourist_id integer NOT NULL,
    tourist_name character(30) NOT NULL,
    tourist_surname character(30) NOT NULL,
    tourist_otch character(30),
    passport character(10),
    city character(30),
    country character(30),
    phone character(13)
);


ALTER TABLE public.tourists OWNER TO postgres;

--
-- TOC entry 220 (class 1259 OID 24596)
-- Name: tourists_tourist_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

ALTER TABLE public.tourists ALTER COLUMN tourist_id ADD GENERATED ALWAYS AS IDENTITY (
    SEQUENCE NAME public.tourists_tourist_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- TOC entry 226 (class 1259 OID 32808)
-- Name: tours_tour_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.tours_tour_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.tours_tour_id_seq OWNER TO postgres;

--
-- TOC entry 217 (class 1259 OID 24577)
-- Name: tours; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.tours (
    tour_id integer DEFAULT nextval('public.tours_tour_id_seq'::regclass) NOT NULL,
    tour_name character(30) NOT NULL,
    price money NOT NULL,
    tour_info text
);


ALTER TABLE public.tours OWNER TO postgres;

--
-- TOC entry 4890 (class 0 OID 24619)
-- Dependencies: 225
-- Data for Name: payment; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.payment (payment_id, putevki_id, payment_date, summa) FROM stdin;
5	10	2025-04-25	50 000,00 ?
6	11	2025-04-25	20 000,00 ?
7	12	2025-04-25	50 000,00 ?
8	10	2025-04-25	10 000,00 ?
9	14	2025-04-25	30 000,00 ?
10	15	2025-04-25	20 000,00 ?
\.


--
-- TOC entry 4888 (class 0 OID 24603)
-- Dependencies: 223
-- Data for Name: putevki; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.putevki (putevki_id, tourist_id, season_id) FROM stdin;
10	5	1
11	1	2
12	2	1
13	3	3
14	4	3
15	4	1
18	7	3
\.


--
-- TOC entry 4884 (class 0 OID 24585)
-- Dependencies: 219
-- Data for Name: seasons; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.seasons (season_id, tour_id, start_date, end_date, closed, amount) FROM stdin;
1	4	2025-04-25	2025-04-27	f	2
2	5	2025-04-16	2025-04-20	f	3
3	6	2025-04-27	2025-04-30	f	4
\.


--
-- TOC entry 4892 (class 0 OID 32834)
-- Dependencies: 227
-- Data for Name: selected_season_id; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.selected_season_id (season_id) FROM stdin;
3
\.


--
-- TOC entry 4886 (class 0 OID 24597)
-- Dependencies: 221
-- Data for Name: tourists; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.tourists (tourist_id, tourist_name, tourist_surname, tourist_otch, passport, city, country, phone) FROM stdin;
1	Иван                          	Иванов                        	Иванович                      	9485984050	Орёл                          	Россия                        	4954098503459
3	Изя                           	Котиков                       	Гамлетович                    	3489573934	Тель-Авив                     	Израиль                       	9475ц93749587
4	Евгения                       	Лукьянченко                   	Олеговна                      	4598798398	Хабаровск                     	Россия                        	4985475987455
5	Альберт                       	Еприкян                       	Сергеевич                     	8798979878	Ташкент                       	Узбекистан                    	9879878767677
7	Лена                          	Головач                       	                              	9876544777	Москва                        	Россия                        	4567899888888
2	Мария                         	Королева                      	Алексеевна                    	4583089453	Кишнёв                        	Польша                        	9459459093450
\.


--
-- TOC entry 4882 (class 0 OID 24577)
-- Dependencies: 217
-- Data for Name: tours; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.tours (tour_id, tour_name, price, tour_info) FROM stdin;
4	Сказачное Бали                	30 000,00 ?	Круто блин
5	Горы Кавказа                  	12 000,00 ?	Лее куда едёшь 
6	Искусство Вернисажа           	5 000,00 ?	Культурно
\.


--
-- TOC entry 4898 (class 0 OID 0)
-- Dependencies: 224
-- Name: payment_payment_id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.payment_payment_id_seq', 10, true);


--
-- TOC entry 4899 (class 0 OID 0)
-- Dependencies: 222
-- Name: putevki_putevki_id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.putevki_putevki_id_seq', 26, true);


--
-- TOC entry 4900 (class 0 OID 0)
-- Dependencies: 218
-- Name: seasons_season_id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.seasons_season_id_seq', 3, true);


--
-- TOC entry 4901 (class 0 OID 0)
-- Dependencies: 220
-- Name: tourists_tourist_id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.tourists_tourist_id_seq', 11, true);


--
-- TOC entry 4902 (class 0 OID 0)
-- Dependencies: 226
-- Name: tours_tour_id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.tours_tour_id_seq', 6, true);


--
-- TOC entry 4731 (class 2606 OID 24623)
-- Name: payment payment_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.payment
    ADD CONSTRAINT payment_pkey PRIMARY KEY (payment_id);


--
-- TOC entry 4729 (class 2606 OID 24607)
-- Name: putevki putevki_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.putevki
    ADD CONSTRAINT putevki_pkey PRIMARY KEY (putevki_id);


--
-- TOC entry 4725 (class 2606 OID 24590)
-- Name: seasons seasons_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.seasons
    ADD CONSTRAINT seasons_pkey PRIMARY KEY (season_id);


--
-- TOC entry 4727 (class 2606 OID 24601)
-- Name: tourists tourists_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.tourists
    ADD CONSTRAINT tourists_pkey PRIMARY KEY (tourist_id);


--
-- TOC entry 4723 (class 2606 OID 24583)
-- Name: tours tours_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.tours
    ADD CONSTRAINT tours_pkey PRIMARY KEY (tour_id);


--
-- TOC entry 4736 (class 2620 OID 24632)
-- Name: tourists trg_add_putevka; Type: TRIGGER; Schema: public; Owner: postgres
--

CREATE TRIGGER trg_add_putevka AFTER INSERT ON public.tourists FOR EACH ROW EXECUTE FUNCTION public.assign_putevka_on_new_tourist();


--
-- TOC entry 4735 (class 2606 OID 24624)
-- Name: payment payment_putevki_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.payment
    ADD CONSTRAINT payment_putevki_id_fkey FOREIGN KEY (putevki_id) REFERENCES public.putevki(putevki_id);


--
-- TOC entry 4733 (class 2606 OID 24613)
-- Name: putevki putevki_season_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.putevki
    ADD CONSTRAINT putevki_season_id_fkey FOREIGN KEY (season_id) REFERENCES public.seasons(season_id);


--
-- TOC entry 4734 (class 2606 OID 24608)
-- Name: putevki putevki_tourist_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.putevki
    ADD CONSTRAINT putevki_tourist_id_fkey FOREIGN KEY (tourist_id) REFERENCES public.tourists(tourist_id);


--
-- TOC entry 4732 (class 2606 OID 24591)
-- Name: seasons seasons_tour_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.seasons
    ADD CONSTRAINT seasons_tour_id_fkey FOREIGN KEY (tour_id) REFERENCES public.tours(tour_id);


-- Completed on 2025-05-02 00:38:22

--
-- PostgreSQL database dump complete
--

