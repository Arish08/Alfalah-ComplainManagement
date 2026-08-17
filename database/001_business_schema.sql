/* Reference schema for the BankingPlatform business database.
   Prefer EF Core migrations for the application so code and schema stay synchronized. */

IF DB_ID(N'BankingPlatform') IS NULL CREATE DATABASE BankingPlatform;
GO
USE BankingPlatform;
GO

CREATE TABLE Departments (
    Id uniqueidentifier NOT NULL PRIMARY KEY,
    Code nvarchar(50) NOT NULL,
    Name nvarchar(200) NOT NULL,
    IsActive bit NOT NULL DEFAULT 1,
    CreatedAtUtc datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAtUtc datetime2 NULL,
    CONSTRAINT UQ_Departments_Code UNIQUE(Code)
);

CREATE TABLE ComplaintCategories (
    Id uniqueidentifier NOT NULL PRIMARY KEY,
    DepartmentId uniqueidentifier NOT NULL,
    Code nvarchar(50) NOT NULL,
    Name nvarchar(200) NOT NULL,
    IsActive bit NOT NULL DEFAULT 1,
    CreatedAtUtc datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAtUtc datetime2 NULL,
    CONSTRAINT FK_ComplaintCategories_Departments FOREIGN KEY (DepartmentId) REFERENCES Departments(Id),
    CONSTRAINT UQ_ComplaintCategories UNIQUE(DepartmentId, Code)
);

CREATE TABLE Users (
    Id uniqueidentifier NOT NULL PRIMARY KEY,
    EmployeeCode nvarchar(50) NOT NULL,
    DisplayName nvarchar(200) NOT NULL,
    Email nvarchar(320) NOT NULL,
    IsActive bit NOT NULL DEFAULT 1,
    CreatedAtUtc datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAtUtc datetime2 NULL,
    CONSTRAINT UQ_Users_EmployeeCode UNIQUE(EmployeeCode),
    CONSTRAINT UQ_Users_Email UNIQUE(Email)
);

CREATE TABLE DepartmentMemberships (
    Id uniqueidentifier NOT NULL PRIMARY KEY,
    UserId uniqueidentifier NOT NULL,
    DepartmentId uniqueidentifier NOT NULL,
    RoleCode nvarchar(80) NOT NULL,
    CreatedAtUtc datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAtUtc datetime2 NULL,
    CONSTRAINT FK_Membership_User FOREIGN KEY(UserId) REFERENCES Users(Id),
    CONSTRAINT FK_Membership_Department FOREIGN KEY(DepartmentId) REFERENCES Departments(Id),
    CONSTRAINT UQ_Membership UNIQUE(UserId, DepartmentId, RoleCode)
);

CREATE TABLE Complaints (
    Id uniqueidentifier NOT NULL PRIMARY KEY,
    ComplaintNumber nvarchar(50) NOT NULL,
    CategoryId uniqueidentifier NOT NULL,
    CustomerReference nvarchar(200) NULL,
    Subject nvarchar(300) NOT NULL,
    Description nvarchar(max) NOT NULL,
    Priority int NOT NULL,
    Status int NOT NULL,
    CreatedByUserId uniqueidentifier NOT NULL,
    CurrentWorkflowInstanceId uniqueidentifier NULL,
    ResolvedAtUtc datetime2 NULL,
    CreatedAtUtc datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAtUtc datetime2 NULL,
    CONSTRAINT UQ_Complaints_Number UNIQUE(ComplaintNumber),
    CONSTRAINT FK_Complaints_Category FOREIGN KEY(CategoryId) REFERENCES ComplaintCategories(Id),
    CONSTRAINT FK_Complaints_CreatedBy FOREIGN KEY(CreatedByUserId) REFERENCES Users(Id)
);

CREATE TABLE WorkflowDefinitions (
    Id uniqueidentifier NOT NULL PRIMARY KEY,
    Name nvarchar(250) NOT NULL,
    DepartmentId uniqueidentifier NOT NULL,
    CategoryId uniqueidentifier NOT NULL,
    Version int NOT NULL,
    Status int NOT NULL,
    DesignerJson nvarchar(max) NOT NULL,
    CreatedByUserId uniqueidentifier NOT NULL,
    PublishedAtUtc datetime2 NULL,
    CreatedAtUtc datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAtUtc datetime2 NULL,
    CONSTRAINT FK_WD_Department FOREIGN KEY(DepartmentId) REFERENCES Departments(Id),
    CONSTRAINT FK_WD_Category FOREIGN KEY(CategoryId) REFERENCES ComplaintCategories(Id),
    CONSTRAINT UQ_WD_CategoryVersion UNIQUE(CategoryId, Version)
);

CREATE TABLE WorkflowNodes (
    Id uniqueidentifier NOT NULL PRIMARY KEY,
    WorkflowDefinitionId uniqueidentifier NOT NULL,
    NodeKey nvarchar(100) NOT NULL,
    Name nvarchar(250) NOT NULL,
    Type int NOT NULL,
    RoleCode nvarchar(80) NULL,
    SlaHours int NULL,
    EscalationRoleCode nvarchar(80) NULL,
    PositionX decimal(18,2) NOT NULL,
    PositionY decimal(18,2) NOT NULL,
    ConfigJson nvarchar(max) NULL,
    CreatedAtUtc datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAtUtc datetime2 NULL,
    CONSTRAINT FK_WN_Definition FOREIGN KEY(WorkflowDefinitionId) REFERENCES WorkflowDefinitions(Id) ON DELETE CASCADE,
    CONSTRAINT UQ_WN_Key UNIQUE(WorkflowDefinitionId, NodeKey)
);

CREATE TABLE WorkflowTransitions (
    Id uniqueidentifier NOT NULL PRIMARY KEY,
    WorkflowDefinitionId uniqueidentifier NOT NULL,
    SourceNodeId uniqueidentifier NOT NULL,
    TargetNodeId uniqueidentifier NOT NULL,
    OutcomeKey nvarchar(100) NULL,
    Label nvarchar(200) NULL,
    CreatedAtUtc datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAtUtc datetime2 NULL,
    CONSTRAINT FK_WT_Definition FOREIGN KEY(WorkflowDefinitionId) REFERENCES WorkflowDefinitions(Id) ON DELETE CASCADE,
    CONSTRAINT FK_WT_Source FOREIGN KEY(SourceNodeId) REFERENCES WorkflowNodes(Id),
    CONSTRAINT FK_WT_Target FOREIGN KEY(TargetNodeId) REFERENCES WorkflowNodes(Id)
);

CREATE TABLE WorkflowInstances (
    Id uniqueidentifier NOT NULL PRIMARY KEY,
    ComplaintId uniqueidentifier NOT NULL,
    WorkflowDefinitionId uniqueidentifier NOT NULL,
    CurrentNodeId uniqueidentifier NULL,
    Status int NOT NULL,
    StartedAtUtc datetime2 NOT NULL,
    CompletedAtUtc datetime2 NULL,
    CreatedAtUtc datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAtUtc datetime2 NULL,
    CONSTRAINT FK_WI_Complaint FOREIGN KEY(ComplaintId) REFERENCES Complaints(Id),
    CONSTRAINT FK_WI_Definition FOREIGN KEY(WorkflowDefinitionId) REFERENCES WorkflowDefinitions(Id),
    CONSTRAINT FK_WI_CurrentNode FOREIGN KEY(CurrentNodeId) REFERENCES WorkflowNodes(Id)
);

ALTER TABLE Complaints ADD CONSTRAINT FK_Complaints_CurrentWorkflowInstance
    FOREIGN KEY(CurrentWorkflowInstanceId) REFERENCES WorkflowInstances(Id);

CREATE TABLE WorkflowTasks (
    Id uniqueidentifier NOT NULL PRIMARY KEY,
    WorkflowInstanceId uniqueidentifier NOT NULL,
    WorkflowNodeId uniqueidentifier NOT NULL,
    ComplaintId uniqueidentifier NOT NULL,
    DepartmentId uniqueidentifier NOT NULL,
    AssignedRoleCode nvarchar(80) NULL,
    AssignedToUserId uniqueidentifier NULL,
    Status int NOT NULL,
    OpenedAtUtc datetime2 NOT NULL,
    DueAtUtc datetime2 NULL,
    EscalatedAtUtc datetime2 NULL,
    CompletedByUserId uniqueidentifier NULL,
    CompletedAtUtc datetime2 NULL,
    CompletionComment nvarchar(max) NULL,
    CreatedAtUtc datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAtUtc datetime2 NULL,
    CONSTRAINT FK_Task_Instance FOREIGN KEY(WorkflowInstanceId) REFERENCES WorkflowInstances(Id),
    CONSTRAINT FK_Task_Node FOREIGN KEY(WorkflowNodeId) REFERENCES WorkflowNodes(Id),
    CONSTRAINT FK_Task_Complaint FOREIGN KEY(ComplaintId) REFERENCES Complaints(Id),
    CONSTRAINT FK_Task_Department FOREIGN KEY(DepartmentId) REFERENCES Departments(Id),
    CONSTRAINT FK_Task_AssignedUser FOREIGN KEY(AssignedToUserId) REFERENCES Users(Id),
    CONSTRAINT FK_Task_CompletedBy FOREIGN KEY(CompletedByUserId) REFERENCES Users(Id)
);
CREATE INDEX IX_WorkflowTasks_StatusDue ON WorkflowTasks(Status, DueAtUtc);
CREATE INDEX IX_WorkflowTasks_AssigneeStatus ON WorkflowTasks(AssignedToUserId, Status);

CREATE TABLE ComplaintEvents (
    Id uniqueidentifier NOT NULL PRIMARY KEY,
    ComplaintId uniqueidentifier NOT NULL,
    ActorUserId uniqueidentifier NULL,
    EventType nvarchar(100) NOT NULL,
    Message nvarchar(max) NOT NULL,
    DataJson nvarchar(max) NULL,
    CreatedAtUtc datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAtUtc datetime2 NULL,
    CONSTRAINT FK_Event_Complaint FOREIGN KEY(ComplaintId) REFERENCES Complaints(Id),
    CONSTRAINT FK_Event_Actor FOREIGN KEY(ActorUserId) REFERENCES Users(Id)
);
CREATE INDEX IX_ComplaintEvents_ComplaintCreated ON ComplaintEvents(ComplaintId, CreatedAtUtc);
GO
