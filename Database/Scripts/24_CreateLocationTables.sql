-- Create master tables for countries, states and cities with seed data for India
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Countries]') AND type = N'U')
BEGIN
    CREATE TABLE [dbo].[Countries]
    (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Name NVARCHAR(150) NOT NULL,
        Code NVARCHAR(10) NOT NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedDate DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
        LastModifiedDate DATETIME2 NOT NULL DEFAULT SYSDATETIME()
    );
END;

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[States]') AND type = N'U')
BEGIN
    CREATE TABLE [dbo].[States]
    (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        CountryId INT NOT NULL,
        Name NVARCHAR(150) NOT NULL,
        Code NVARCHAR(10) NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedDate DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
        LastModifiedDate DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
        CONSTRAINT FK_States_Countries FOREIGN KEY (CountryId) REFERENCES Countries(Id)
    );
END;

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Cities]') AND type = N'U')
BEGIN
    CREATE TABLE [dbo].[Cities]
    (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        StateId INT NOT NULL,
        Name NVARCHAR(150) NOT NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedDate DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
        LastModifiedDate DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
        CONSTRAINT FK_Cities_States FOREIGN KEY (StateId) REFERENCES States(Id)
    );
END;

-- Ensure India exists in master country list
IF NOT EXISTS (SELECT 1 FROM Countries WHERE Code = 'IN')
BEGIN
    INSERT INTO Countries (Name, Code, IsActive)
    VALUES ('India', 'IN', 1);
END;

DECLARE @IndiaId INT = (SELECT TOP 1 Id FROM Countries WHERE Code = 'IN');

IF @IndiaId IS NOT NULL
BEGIN
    DECLARE @StateData TABLE (Name NVARCHAR(150), Code NVARCHAR(10));
    INSERT INTO @StateData (Name, Code)
    VALUES
        ('Andhra Pradesh', 'AP'),
        ('Arunachal Pradesh', 'AR'),
        ('Assam', 'AS'),
        ('Bihar', 'BR'),
        ('Chhattisgarh', 'CT'),
        ('Goa', 'GA'),
        ('Gujarat', 'GJ'),
        ('Haryana', 'HR'),
        ('Himachal Pradesh', 'HP'),
        ('Jharkhand', 'JH'),
        ('Karnataka', 'KA'),
        ('Kerala', 'KL'),
        ('Madhya Pradesh', 'MP'),
        ('Maharashtra', 'MH'),
        ('Manipur', 'MN'),
        ('Meghalaya', 'ML'),
        ('Mizoram', 'MZ'),
        ('Nagaland', 'NL'),
        ('Odisha', 'OD'),
        ('Punjab', 'PB'),
        ('Rajasthan', 'RJ'),
        ('Sikkim', 'SK'),
        ('Tamil Nadu', 'TN'),
        ('Telangana', 'TS'),
        ('Tripura', 'TR'),
        ('Uttar Pradesh', 'UP'),
        ('Uttarakhand', 'UK'),
        ('West Bengal', 'WB'),
        ('Andaman and Nicobar Islands', 'AN'),
        ('Chandigarh', 'CH'),
        ('Dadra and Nagar Haveli and Daman and Diu', 'DNHDD'),
        ('Delhi', 'DL'),
        ('Jammu and Kashmir', 'JK'),
        ('Ladakh', 'LA'),
        ('Lakshadweep', 'LD'),
        ('Puducherry', 'PY');

    INSERT INTO States (CountryId, Name, Code, IsActive)
    SELECT @IndiaId, s.Name, s.Code, 1
    FROM @StateData s
    WHERE NOT EXISTS (
        SELECT 1 FROM States st
        WHERE st.CountryId = @IndiaId AND st.Name = s.Name
    );

    DECLARE @CityData TABLE (StateName NVARCHAR(150), CityName NVARCHAR(150));
    INSERT INTO @CityData (StateName, CityName)
    VALUES
        ('Andhra Pradesh', 'Visakhapatnam'),
        ('Andhra Pradesh', 'Vijayawada'),
        ('Andhra Pradesh', 'Guntur'),
        ('Arunachal Pradesh', 'Itanagar'),
        ('Arunachal Pradesh', 'Tawang'),
        ('Arunachal Pradesh', 'Ziro'),
        ('Assam', 'Guwahati'),
        ('Assam', 'Silchar'),
        ('Assam', 'Dibrugarh'),
        ('Bihar', 'Patna'),
        ('Bihar', 'Gaya'),
        ('Bihar', 'Bhagalpur'),
        ('Chhattisgarh', 'Raipur'),
        ('Chhattisgarh', 'Bilaspur'),
        ('Chhattisgarh', 'Durg'),
        ('Goa', 'Panaji'),
        ('Goa', 'Margao'),
        ('Goa', 'Vasco da Gama'),
        ('Gujarat', 'Ahmedabad'),
        ('Gujarat', 'Surat'),
        ('Gujarat', 'Vadodara'),
        ('Haryana', 'Gurugram'),
        ('Haryana', 'Faridabad'),
        ('Haryana', 'Panipat'),
        ('Himachal Pradesh', 'Shimla'),
        ('Himachal Pradesh', 'Dharamshala'),
        ('Himachal Pradesh', 'Manali'),
        ('Jharkhand', 'Ranchi'),
        ('Jharkhand', 'Jamshedpur'),
        ('Jharkhand', 'Dhanbad'),
        ('Karnataka', 'Bengaluru'),
        ('Karnataka', 'Mysuru'),
        ('Karnataka', 'Mangaluru'),
        ('Kerala', 'Thiruvananthapuram'),
        ('Kerala', 'Kochi'),
        ('Kerala', 'Kozhikode'),
        ('Madhya Pradesh', 'Bhopal'),
        ('Madhya Pradesh', 'Indore'),
        ('Madhya Pradesh', 'Gwalior'),
        ('Maharashtra', 'Mumbai'),
        ('Maharashtra', 'Pune'),
        ('Maharashtra', 'Nagpur'),
        ('Manipur', 'Imphal'),
        ('Manipur', 'Churachandpur'),
        ('Manipur', 'Thoubal'),
        ('Meghalaya', 'Shillong'),
        ('Meghalaya', 'Tura'),
        ('Meghalaya', 'Jowai'),
        ('Mizoram', 'Aizawl'),
        ('Mizoram', 'Lunglei'),
        ('Mizoram', 'Champhai'),
        ('Nagaland', 'Kohima'),
        ('Nagaland', 'Dimapur'),
        ('Nagaland', 'Mokokchung'),
        ('Odisha', 'Bhubaneswar'),
        ('Odisha', 'Cuttack'),
        ('Odisha', 'Rourkela'),
        ('Punjab', 'Chandigarh'),
        ('Punjab', 'Ludhiana'),
        ('Punjab', 'Amritsar'),
        ('Rajasthan', 'Jaipur'),
        ('Rajasthan', 'Udaipur'),
        ('Rajasthan', 'Jodhpur'),
        ('Sikkim', 'Gangtok'),
        ('Sikkim', 'Namchi'),
        ('Sikkim', 'Gyalshing'),
        ('Tamil Nadu', 'Chennai'),
        ('Tamil Nadu', 'Coimbatore'),
        ('Tamil Nadu', 'Madurai'),
        ('Telangana', 'Hyderabad'),
        ('Telangana', 'Warangal'),
        ('Telangana', 'Nizamabad'),
        ('Tripura', 'Agartala'),
        ('Tripura', 'Dharmanagar'),
        ('Tripura', 'Udaipur'),
        ('Uttar Pradesh', 'Lucknow'),
        ('Uttar Pradesh', 'Kanpur'),
        ('Uttar Pradesh', 'Varanasi'),
        ('Uttarakhand', 'Dehradun'),
        ('Uttarakhand', 'Haridwar'),
        ('Uttarakhand', 'Haldwani'),
        ('West Bengal', 'Kolkata'),
        ('West Bengal', 'Siliguri'),
        ('West Bengal', 'Durgapur'),
        ('Andaman and Nicobar Islands', 'Port Blair'),
        ('Chandigarh', 'Chandigarh'),
        ('Dadra and Nagar Haveli and Daman and Diu', 'Daman'),
        ('Dadra and Nagar Haveli and Daman and Diu', 'Silvassa'),
        ('Delhi', 'New Delhi'),
        ('Delhi', 'Dwarka'),
        ('Delhi', 'Rohini'),
        ('Jammu and Kashmir', 'Srinagar'),
        ('Jammu and Kashmir', 'Jammu'),
        ('Jammu and Kashmir', 'Anantnag'),
        ('Ladakh', 'Leh'),
        ('Ladakh', 'Kargil'),
        ('Lakshadweep', 'Kavaratti'),
        ('Lakshadweep', 'Agatti'),
        ('Puducherry', 'Puducherry'),
        ('Puducherry', 'Karaikal'),
        ('Puducherry', 'Mahe');

    INSERT INTO Cities (StateId, Name, IsActive)
    SELECT st.Id, cd.CityName, 1
    FROM @CityData cd
    INNER JOIN States st
        ON st.Name = cd.StateName AND st.CountryId = @IndiaId
    WHERE NOT EXISTS (
        SELECT 1 FROM Cities c
        WHERE c.StateId = st.Id AND c.Name = cd.CityName
    );
END;
