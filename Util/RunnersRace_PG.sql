CREATE TABLE "Events" (
  "IDEvent" integer PRIMARY KEY,
  "DateEvent" datetime,
  "DescEvent" varchar(200),
  "LenghtEv" integer,
  "TypeEv" varchar,
  "ShortCirc" boolean
);

CREATE TABLE "Categories" (
  "IDCat" integer PRIMARY KEY,
  "DesCat" VARCHAR(200),
  "SexCat" VARCHAR(1),
  "YearBase" integer,
  "YearFrom" integer,
  "YearTo" integer,
  "ShortCircCat" boolean
);

CREATE TABLE "Runners" (
  "RunID" integer PRIMARY KEY,
  "Name" varchar(200),
  "BirthYear" integer,
  "Sex" varchar(1),
  "Event_id" integer,
  "Category_id" integer,
  "RaceNumber" integer
);

CREATE TABLE "NumRaceEpc" (
  "RaceNumber" integer,
  "EPC" varchar,
  "InUse" varchar
);

CREATE TABLE "Timings" (
  "TimeID" integer PRIMARY KEY,
  "Event_id" integer,
  "RunID" integer,
  "RaceNumber" integer,
  "EPC" varchar,
  "StartTime" datetime,
  "EndTime" datetime,
  "ElapsedTime" datetime,
  "Modified" boolean
);

CREATE TABLE "Rankings" (
  "RankID" integer,
  "Event_id" integer,
  "RunID" integer,
  "ElapsedTime" datetime,
  "Category_id" integer,
  "Team" integer
);

CREATE TABLE "RelayRace" (
  "RRID" integer,
  "Event_id" integer,
  "RunID" integer,
  "Team" integer
);

ALTER TABLE "Events" ADD FOREIGN KEY ("IDEvent") REFERENCES "Runners" ("Event_id");

ALTER TABLE "Events" ADD FOREIGN KEY ("IDEvent") REFERENCES "Timings" ("Event_id");

ALTER TABLE "Runners" ADD FOREIGN KEY ("Category_id") REFERENCES "Categories" ("IDCat");

ALTER TABLE "NumRaceEpc" ADD FOREIGN KEY ("RaceNumber") REFERENCES "Runners" ("RaceNumber");

ALTER TABLE "Timings" ADD FOREIGN KEY ("RunID") REFERENCES "Runners" ("RunID");

ALTER TABLE "Rankings" ADD FOREIGN KEY ("Event_id") REFERENCES "Timings" ("Event_id");

ALTER TABLE "Rankings" ADD FOREIGN KEY ("RunID") REFERENCES "Timings" ("RunID");

ALTER TABLE "RelayRace" ADD FOREIGN KEY ("Event_id") REFERENCES "Events" ("IDEvent");

ALTER TABLE "RelayRace" ADD FOREIGN KEY ("RunID") REFERENCES "Runners" ("RunID");

ALTER TABLE "Timings" ADD FOREIGN KEY ("RaceNumber") REFERENCES "Runners" ("RaceNumber");
