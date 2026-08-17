USE BankingPlatform;
GO

IF NOT EXISTS (SELECT 1 FROM Departments WHERE Id='aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa')
INSERT INTO Departments(Id,Code,Name,IsActive,CreatedAtUtc) VALUES
('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa','CMU','Complaint Management Unit',1,SYSUTCDATETIME());

IF NOT EXISTS (SELECT 1 FROM ComplaintCategories WHERE Id='bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb')
BEGIN
INSERT INTO ComplaintCategories(Id,DepartmentId,Code,Name,IsActive,CreatedAtUtc) VALUES
('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb','aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa','ATM','ATM Issue',1,SYSUTCDATETIME()),
('cccccccc-cccc-cccc-cccc-cccccccccccc','aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa','ALFA_MALL','Alfa Mall',1,SYSUTCDATETIME()),
('dddddddd-dddd-dddd-dddd-dddddddddddd','aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa','TRANSACTION','Transaction Issue',1,SYSUTCDATETIME());
END

IF NOT EXISTS (SELECT 1 FROM Users WHERE Id='11111111-1111-1111-1111-111111111111')
BEGIN
INSERT INTO Users(Id,EmployeeCode,DisplayName,Email,IsActive,CreatedAtUtc) VALUES
('11111111-1111-1111-1111-111111111111','UH001','Unit Head','unithead@bank.local',1,SYSUTCDATETIME()),
('22222222-2222-2222-2222-222222222222','TL001','Team Lead','teamlead@bank.local',1,SYSUTCDATETIME()),
('33333333-3333-3333-3333-333333333333','OF001','Officer One','officer1@bank.local',1,SYSUTCDATETIME()),
('44444444-4444-4444-4444-444444444444','OF002','Officer Two','officer2@bank.local',1,SYSUTCDATETIME());

INSERT INTO DepartmentMemberships(Id,UserId,DepartmentId,RoleCode,CreatedAtUtc) VALUES
(NEWID(),'11111111-1111-1111-1111-111111111111','aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa','UnitHead',SYSUTCDATETIME()),
(NEWID(),'11111111-1111-1111-1111-111111111111','aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa','DeptAdmin',SYSUTCDATETIME()),
(NEWID(),'22222222-2222-2222-2222-222222222222','aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa','TeamLead',SYSUTCDATETIME()),
(NEWID(),'33333333-3333-3333-3333-333333333333','aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa','Officer',SYSUTCDATETIME()),
(NEWID(),'44444444-4444-4444-4444-444444444444','aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa','Officer',SYSUTCDATETIME());
END
GO
