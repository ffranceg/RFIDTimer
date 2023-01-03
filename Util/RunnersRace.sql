----------------------------------------------------------------------------------
----------------------------------------------------------------------------------
--        // Events EV
--        // Runners RU
--        // EventRunner ER
--        // Timings TG
--        // NumRaceEpc NC
--        // Rankings RK
--        // RelayRace RR
--        // Categories CT
----------------------------------------------------------------------------------
Drop Table Events;
CREATE TABLE [Events] (
  [IDEvent] integer PRIMARY KEY AUTOINCREMENT,
  [DateEvent] datetime,
  [DescEvent] varchar(200),
  [LenghtEv] integer,
  [TypeEv] nvarchar(50),
  [ShortCirc] boolean,
  FOREIGN KEY ([IDEvent]) REFERENCES [EventRunner] ([Event_id])
);
INSERT INTO Events (DateEvent,  DescEvent,  LenghtEv,  TypeEv,  ShortCirc)
VALUES
  ("2023-07-23 10:30:00",'CaF 2023 - Adulti 2000Mt',2000,'Adulti',false),
  ("2023-07-23 10:00:00",'CaF 2023 - Bambini 730Mt',730,'Bambini',true),
  ("2023-07-23 11:00:00",'Staffetta mista 2000Mt',2000,'Staffetta',false);
----------------------------------------------------------------------------------


----------------------------------------------------------------------------------
Drop Table Categories;
CREATE TABLE [Categories] (
  [IDCat] integer PRIMARY KEY AUTOINCREMENT,
  [DesCat] VARCHAR(200),
  [SexCat] VARCHAR(1),
  [YearFrom] integer,
  [YearTo] integer,
  [ShortCircCat] boolean
);

INSERT INTO Categories (DesCat,SexCat,YearFrom,YearTo,ShortCircCat) 
VALUES
  ('Cuccioli M','M',0,5,1),
  ('Cuccioli F','F',0,5,1),
  ('Esordienti M','M',6,11,1),
  ('Esordienti F','F',6,11,1),
  ('Ragazzi M','M',12,13,0),
  ('Ragazzi F','F',12,13,0),
  ('Cadetti M','M',14,15,0),
  ('Cadetti F','F',14,15,0),
  ('Allievi M','M',16,17,0),
  ('Allievi F','F',16,17,0),
  ('Juniores M','M',18,19,0),
  ('Juniores F','F',18,19,0),
  ('Promesse M','M',20,22,0),
  ('Promesse F','F',20,22,0),
  ('Seniores M','M',23,34,0),
  ('Seniores F','F',23,34,0),
  ('SM35','M',35,39,0),
  ('SF35','F',35,39,0),
  ('SM40','M',40,44,0),
  ('SF40','F',40,44,0),
  ('SM45','M',45,49,0),
  ('SF45','F',45,49,0),
  ('SM50','M',50,54,0),
  ('SF50','F',50,54,0),
  ('SM55','M',55,59,0),
  ('SF55','F',55,59,0),
  ('SM60','M',60,64,0),
  ('SF60','F',60,64,0),
  ('SM65','M',65,69,0),
  ('SF65','F',65,69,0),
  ('SM70','M',70,74,0),
  ('SF70','F',70,74,0),
  ('SM75','M',75,79,0),
  ('SF75','F',75,79,0),
  ('SM80','M',80,85,0),
  ('SF80','F',80,85,0),
  ('SM85','M',85,89,0),
  ('SF85','F',85,89,0);
----------------------------------------------------------------------------------
Drop Table Runners;
CREATE TABLE [Runners] (
  [IDRun] integer PRIMARY KEY AUTOINCREMENT,
  [Name] varchar(200),
  [BirthYear] integer,
  [Sex] varchar(1),
  [Email] varchar(200),
  FOREIGN KEY ([IDRun]) REFERENCES [EventRunner] ([RunID])
);
INSERT INTO SQLITE_SEQUENCE (name,seq) values ('Runners',100);
UPDATE SQLITE_SEQUENCE SET seq = 100 WHERE name = 'Runners';

INSERT INTO Runners
(Name,BirthYear,Sex)
VALUES
  ('Casale Alessio',2010,'M'),
  ('Casaleggio Luca',1974,'M'),
  ('Garaventa Anna',1972,'F'),
  ('Garaventa Luca',1993,'M'),
  ('Magillo Elena',1999,'F'),
  ('Magillo Elisa',2005,'F'),
  ('Parodi Greta',2012,'F'),
  ('Rosa Matilde',2013,'F'),
  ('Rissolio Alice',2006,'F'),
  ('De Turris Sabrina',1974,'F'),
  ('Romano Alessia',2012,'F'),
  ('Penserini Beatrice',2000,'F'),
  ('Garaventa Andrea Pippi',2002,'M'),
  ('Russo Francesco',2010,'M'),
  ('Russo Massimiliano',1974,'M'),
  ('Rissolio Alessandro',1971,'M'),
  ('Schenone Luca',2000,'M'),
  ('Bulgaresi Riccardo',1964,'M'),
  ('Diassise Francesco',1967,'M'),
  ('Patrucco Umberto',1963,'M'),
  ('Romano Giada',2016,'F'),
  ('Rosa Alice',2017,'F'),
  ('Gimorri Federico',1997,'M'),
  ('Casaleggio Davide',2012,'M'),
  ('Rosa Maria Paola',1971,'F'),
  ('Allara Federica',2003,'F'),
  ('Battista Alessandro',2009,'M'),
  ('Fortes Fernandes Eritson',2006,'M'),
  ('Fortes Gomes Rodrigues Maximiliana',1984,'F'),
  ('Morabito Lorenzo',2017,'M'),
  ('Rissolio Federico',2002,'M'),
  ('Conti Giammario',1968,'M'),
  ('Battista Massimo',1970,'M'),
  ('Bozzolo Edoardo',1999,'M'),
  ('D''Agostino Andrea',1999,'M'),
  ('Galluccio Arianna',2000,'F'),
  ('Gregory Giorgia',2005,'F'),
  ('Gregory Marta',2009,'F'),
  ('Mantero Laura',1975,'F'),
  ('Maramaldo Federica',1981,'F'),
  ('Mazzucco Giovanna',1949,'F'),
  ('Pace Aurora',2008,'F'),
  ('Parodi Moreno',1968,'M'),
  ('Rosa Pietro',2002,'M'),
  ('Tocci Giulia',2012,'F'),
  ('Tocci Massimo',1977,'M'),
  ('Tocci Matilde',2015,'F'),
  ('Coviello Matteo',1985,'M'),
  ('Marta Bertamino',1985,'F'),
  ('La Rocca Giuseppina',1960,'F'),
  ('Bertamino Giovanni',1958,'M'),
  ('Coviello Domenico',1956,'M'),
  ('Coviello Giacomo',2020,'M'),
  ('Coviello Davide',2018,'M'),
  ('Coviello Giovanni',2015,'M'),
  ('Carbone Lorenzo',1996,'M'),
  ('Cabone Martina',2002,'F'),
  ('Capra Filippo',2011,'M'),
  ('Santos Martins Florindo',1978,'M'),
  ('Pastorini Elisabetta',1972,'F'),
  ('Calcagno Margherita',2011,'F'),
  ('Bazzotti Keylor',2000,'M'),
  ('Celotto Alberto',1971,'M'),
  ('Missio Asia',2012,'F'),
  ('Celotto Camilla',2005,'F'),
  ('Carotenuto Claudia',2015,'F'),
  ('Frisone Agnese',2014,'F'),
  ('Frisone Francesco',2010,'M'),
  ('Pitto Sofia',2012,'F'),
  ('Gatti Federico',2005,'M'),
  ('Bertamino Marta',1985,'F'),
  ('Pittari Giada',2019,'F'),
  ('Fregatti Mattia',2020,'M'),
  ('Fregatti Tommaso',1977,'M'),
  ('Morabito Simone',2018,'M');
----------------------------------------------------------------------------------
drop Table EventRunner;
delete from EventRunner;
CREATE Table EventRunner (
  Event_id integer,
  Runner_id integer,
  Category_id integer,
  RaceNumber integer,
  FOREIGN KEY ([Category_id]) REFERENCES [Categories] ([IDCat]),
  FOREIGN KEY (Event_id) REFERENCES Timings (Event_id),
  FOREIGN KEY (Runner_id) REFERENCES Runners (IDRun)
);
select * from EventRunner;
select EV.DateEvent,EV.DescEvent,EV.LenghtEv,EV.TypeEv,EV.ShortCirc,ER.RaceNumber,RU.Name,RU.BirthYear,RU.Sex,CT.DesCat,CT.SexCat,CT.ShortCircCat 
from EventRunner as ER 
inner join Events EV on EV.IDEvent = ER.event_id 
inner join Runners RU on RU.IDRun = ER.Runner_id 
inner join Categories CT on CT.IDCat = ER.Category_id ;
;
insert into EventRunner select 1 as event_id, R.IDRun, C.IDCat, (select min(Number) from NumRaceEpc where Number not in (select RaceNumber from EventRunner)) as RaceNumber from Runners R inner join Categories C on C.SexCat = R.Sex and ((cast(strftime('%Y','now') as integer)-R.BirthYear) between YearFrom and YearTo) where R.IDRun = 106;
insert into EventRunner select 2 as event_id, R.IDRun, C.IDCat, null as RaceNumber from Runners R inner join Categories C on C.SexCat = R.Sex and ((cast(strftime('%Y','now') as integer)-R.BirthYear) between YearFrom and YearTo) where R.IDRun = 104;
----------------------------------------------------------------------------------
Drop Table NumRaceEpc;
CREATE TABLE [NumRaceEpc] (
  [Number] integer,
  [EPC] nvarchar(40),
  [InUse] boolean,
  FOREIGN KEY ([Number]) REFERENCES [Runners] ([RaceNumber])
);

INSERT INTO NumRaceEpc (Number,EPC,InUse) 
VALUES
  (1,'E2801160600002136009204E',true),
  (2,'E2801160600002136008F46E',true),
  (3,'E2801160600002136008C8BE',true),
  (4,'E280116060000213600892BE',true),
  (5,'E280116060000213600892CE',true),
  (6,'E2801160600002136008FACE',true),
  (7,'E2801160600002136008C8DE',true),
  (8,'E2801160600002136008FABE',true),
  (9,'E28011606000021360094A3D',true),
  (10,'E2801160600002136008F45E',true),
  (11,'E2801160600002136008F44E',true),
  (12,'E2801160600002136008F43E',true),
  (13,'E280116060000213600892DE',true),
  (14,'E280116060000213600892DE',true),
  (15,'E280116060000213600892EE',true),
  (16,'E2801160600002136009203E',true),
  (17,'E2801160600002136008C8EE',true),
  (18,'E2801160600002136008C8CE',true),
  (19,'E2801160600002136008C8CE',true),
  (20,'E2801160600002136008C84E',true);

----------------------------------------------------------------------------------
Drop Table Timings;
CREATE TABLE [Timings] (
  [IDTime] integer PRIMARY KEY AUTOINCREMENT,
  [Event_id] integer,
  [Runner_id] integer,
  [RaceNumber] integer,
  [EPC] nvarchar(255),
  [StartTime] datetime,
  [EndTime] datetime,
  [ElapsedTime] datetime,
  [Modified] boolean,
  FOREIGN KEY ([Runner_id]) REFERENCES [Runners] ([IDRun]),
  FOREIGN KEY ([RaceNumber]) REFERENCES [Runners] ([RaceNumber])
);


----------------------------------------------------------------------------------
Drop Table Rankings;
CREATE TABLE [Rankings] (
  [RankID] integer PRIMARY KEY AUTOINCREMENT,
  [Event_id] integer,
  [Runnner_id] integer,
  [ElapsedTime] datetime,
  [Category_id] integer,
  [Team] varchar(20),
  FOREIGN KEY ([Event_id]) REFERENCES [Timings] ([Event_id]),
  FOREIGN KEY ([Runnner_id]) REFERENCES [Timings] ([Runnner_id])
);


----------------------------------------------------------------------------------
Drop Table RelayRace;
CREATE TABLE [RelayRace] (
  [RRID] integer PRIMARY KEY AUTOINCREMENT,
  [Event_id] integer,
  [RunID] integer,
  [Team] varchar(20),
  FOREIGN KEY ([Event_id]) REFERENCES [Events] ([IDEvent]),
  FOREIGN KEY ([RunID]) REFERENCES [Runners] ([RunID])
);





