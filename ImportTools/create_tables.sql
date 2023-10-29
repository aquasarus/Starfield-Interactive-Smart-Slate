CREATE TABLE VersionInfo (VersionNumber INT);
INSERT INTO VersionInfo (VersionNumber) VALUES (1);

CREATE TABLE Systems (
                                    SystemID INTEGER,
                                    SystemName TEXT NOT NULL UNIQUE,
                                    SystemLevel INTEGER NOT NULL,
                                    Discovered INTEGER NOT NULL DEFAULT 0,
                                    PRIMARY KEY(SystemID));

CREATE TABLE CelestialBodies (
                                    BodyID INTEGER,
                                    BodyName TEXT NOT NULL,
                                    IsMoon INTEGER NOT NULL,
                                    BodyType TEXT,
                                    Gravity REAL,
                                    Temperature TEXT,
                                    Atmosphere TEXT,
                                    Magnetosphere TEXT,
                                    Water TEXT,
                                    TotalFauna INTEGER NOT NULL DEFAULT 0,
                                    TotalFlora INTEGER NOT NULL DEFAULT 0,
                                    PRIMARY KEY(BodyID));

CREATE TABLE SystemBodies (
                                    SystemID INTEGER NOT NULL,
                                    BodyID INTEGER NOT NULL,
                                    FOREIGN KEY(BodyID) REFERENCES CelestialBodies(BodyID),
                                    FOREIGN KEY(SystemID) REFERENCES Systems(SystemID),
                                    PRIMARY KEY(SystemID,BodyID));

CREATE TABLE PlanetMoons (
                                    PlanetID INTEGER NOT NULL,
                                    MoonID INTEGER NOT NULL,
                                    FOREIGN KEY(PlanetID) REFERENCES CelestialBodies(BodyID),
                                    FOREIGN KEY(MoonID) REFERENCES CelestialBodies(BodyID),
                                    PRIMARY KEY(PlanetID, MoonID));

CREATE TABLE Resources (
                                    ResourceID INTEGER PRIMARY KEY,
                                    ResourceName TEXT NOT NULL,
                                    ResourceType INTEGER NOT NULL,
                                    ResourceShortName TEXT,
                                    ResourceRarity INTEGER NOT NULL);

CREATE TABLE BodyResources (
                                    BodyID INTEGER,
                                    ResourceID INTEGER,
                                    FOREIGN KEY (BodyID) REFERENCES CelestialBodies(BodyID),
                                    FOREIGN KEY (ResourceID) REFERENCES Resources(ResourceID),
                                    PRIMARY KEY (BodyID, ResourceID));

CREATE TABLE LifeformNames (
                                    LifeformName TEXT PRIMARY KEY,
                                    LifeformType INTEGER NOT NULL);

CREATE TABLE Faunas (
                                    FaunaID INTEGER PRIMARY KEY,
                                    FaunaName TEXT NOT NULL,
                                    ParentBodyID INTEGER NOT NULL,
                                    FaunaNotes TEXT,
                                    FOREIGN KEY (ParentBodyID) REFERENCES CelestialBodies(BodyID));

CREATE TABLE FaunaResources (
                                    FaunaID INTEGER,
                                    ResourceID INTEGER,
                                    DropRate INTEGER NOT NULL DEFAULT 0,
                                    FOREIGN KEY (FaunaID) REFERENCES Faunas(FaunaID),
                                    FOREIGN KEY (ResourceID) REFERENCES Resources(ResourceID),
                                    PRIMARY KEY (FaunaID, ResourceID));

CREATE TABLE Floras (
                                    FloraID INTEGER PRIMARY KEY,
                                    FloraName TEXT NOT NULL,
                                    ParentBodyID INTEGER NOT NULL,
                                    FloraNotes TEXT,
                                    FOREIGN KEY (ParentBodyID) REFERENCES CelestialBodies(BodyID));

CREATE TABLE FloraResources (
                                    FloraID INTEGER,
                                    ResourceID INTEGER,
                                    DropRate INTEGER NOT NULL DEFAULT 0,
                                    FOREIGN KEY (FloraID) REFERENCES Floras(FloraID),
                                    FOREIGN KEY (ResourceID) REFERENCES Resources(ResourceID),
                                    PRIMARY KEY (FloraID, ResourceID));

CREATE TABLE FaunaPictures (
                                    FaunaPictureID INTEGER PRIMARY KEY,
                                    FaunaID INTEGER,
                                    FaunaPicturePath TEXT NOT NULL,
                                    FOREIGN KEY (FaunaID) REFERENCES Faunas(FaunaID));

CREATE TABLE FloraPictures (
                                    FloraPictureID INTEGER PRIMARY KEY,
                                    FloraID INTEGER,
                                    FloraPicturePath TEXT NOT NULL,
                                    FOREIGN KEY (FloraID) REFERENCES Floras(FloraID));
