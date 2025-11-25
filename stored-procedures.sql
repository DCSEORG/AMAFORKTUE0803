-- Stored Procedures for Expense Management System
-- All database operations go through these procedures
-- App code should NOT use direct SQL queries

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- =============================================
-- GET ALL EXPENSES (with optional filters)
-- =============================================
CREATE OR ALTER PROCEDURE [dbo].[usp_GetExpenses]
    @StatusFilter NVARCHAR(50) = NULL,
    @CategoryFilter NVARCHAR(100) = NULL,
    @UserId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        e.ExpenseId,
        e.UserId,
        u.UserName,
        u.Email,
        e.CategoryId,
        c.CategoryName,
        e.StatusId,
        s.StatusName,
        CAST(e.AmountMinor / 100.0 AS DECIMAL(10,2)) AS Amount,
        e.Currency,
        e.ExpenseDate,
        e.Description,
        e.ReceiptFile,
        e.SubmittedAt,
        e.ReviewedBy,
        reviewer.UserName AS ReviewerName,
        e.ReviewedAt,
        e.CreatedAt
    FROM dbo.Expenses e
    INNER JOIN dbo.Users u ON e.UserId = u.UserId
    INNER JOIN dbo.ExpenseCategories c ON e.CategoryId = c.CategoryId
    INNER JOIN dbo.ExpenseStatus s ON e.StatusId = s.StatusId
    LEFT JOIN dbo.Users reviewer ON e.ReviewedBy = reviewer.UserId
    WHERE (@StatusFilter IS NULL OR s.StatusName = @StatusFilter)
      AND (@CategoryFilter IS NULL OR c.CategoryName = @CategoryFilter)
      AND (@UserId IS NULL OR e.UserId = @UserId)
    ORDER BY e.ExpenseDate DESC, e.CreatedAt DESC;
END
GO

-- =============================================
-- GET SINGLE EXPENSE BY ID
-- =============================================
CREATE OR ALTER PROCEDURE [dbo].[usp_GetExpenseById]
    @ExpenseId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        e.ExpenseId,
        e.UserId,
        u.UserName,
        u.Email,
        e.CategoryId,
        c.CategoryName,
        e.StatusId,
        s.StatusName,
        CAST(e.AmountMinor / 100.0 AS DECIMAL(10,2)) AS Amount,
        e.Currency,
        e.ExpenseDate,
        e.Description,
        e.ReceiptFile,
        e.SubmittedAt,
        e.ReviewedBy,
        reviewer.UserName AS ReviewerName,
        e.ReviewedAt,
        e.CreatedAt
    FROM dbo.Expenses e
    INNER JOIN dbo.Users u ON e.UserId = u.UserId
    INNER JOIN dbo.ExpenseCategories c ON e.CategoryId = c.CategoryId
    INNER JOIN dbo.ExpenseStatus s ON e.StatusId = s.StatusId
    LEFT JOIN dbo.Users reviewer ON e.ReviewedBy = reviewer.UserId
    WHERE e.ExpenseId = @ExpenseId;
END
GO

-- =============================================
-- CREATE NEW EXPENSE
-- =============================================
CREATE OR ALTER PROCEDURE [dbo].[usp_CreateExpense]
    @UserId INT,
    @CategoryId INT,
    @Amount DECIMAL(10,2),
    @ExpenseDate DATE,
    @Description NVARCHAR(1000) = NULL,
    @ReceiptFile NVARCHAR(500) = NULL,
    @ExpenseId INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @AmountMinor INT = CAST(@Amount * 100 AS INT);
    DECLARE @DraftStatusId INT;
    
    SELECT @DraftStatusId = StatusId FROM dbo.ExpenseStatus WHERE StatusName = 'Draft';
    
    INSERT INTO dbo.Expenses (UserId, CategoryId, StatusId, AmountMinor, Currency, ExpenseDate, Description, ReceiptFile, CreatedAt)
    VALUES (@UserId, @CategoryId, @DraftStatusId, @AmountMinor, 'GBP', @ExpenseDate, @Description, @ReceiptFile, SYSUTCDATETIME());
    
    SET @ExpenseId = SCOPE_IDENTITY();
    
    SELECT @ExpenseId AS ExpenseId;
END
GO

-- =============================================
-- UPDATE EXPENSE
-- =============================================
CREATE OR ALTER PROCEDURE [dbo].[usp_UpdateExpense]
    @ExpenseId INT,
    @CategoryId INT,
    @Amount DECIMAL(10,2),
    @ExpenseDate DATE,
    @Description NVARCHAR(1000) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @AmountMinor INT = CAST(@Amount * 100 AS INT);
    
    UPDATE dbo.Expenses
    SET CategoryId = @CategoryId,
        AmountMinor = @AmountMinor,
        ExpenseDate = @ExpenseDate,
        Description = @Description
    WHERE ExpenseId = @ExpenseId;
    
    SELECT @@ROWCOUNT AS RowsAffected;
END
GO

-- =============================================
-- DELETE EXPENSE
-- =============================================
CREATE OR ALTER PROCEDURE [dbo].[usp_DeleteExpense]
    @ExpenseId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    DELETE FROM dbo.Expenses WHERE ExpenseId = @ExpenseId;
    
    SELECT @@ROWCOUNT AS RowsAffected;
END
GO

-- =============================================
-- SUBMIT EXPENSE FOR APPROVAL
-- =============================================
CREATE OR ALTER PROCEDURE [dbo].[usp_SubmitExpense]
    @ExpenseId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @SubmittedStatusId INT;
    SELECT @SubmittedStatusId = StatusId FROM dbo.ExpenseStatus WHERE StatusName = 'Submitted';
    
    UPDATE dbo.Expenses
    SET StatusId = @SubmittedStatusId,
        SubmittedAt = SYSUTCDATETIME()
    WHERE ExpenseId = @ExpenseId;
    
    SELECT @@ROWCOUNT AS RowsAffected;
END
GO

-- =============================================
-- APPROVE EXPENSE
-- =============================================
CREATE OR ALTER PROCEDURE [dbo].[usp_ApproveExpense]
    @ExpenseId INT,
    @ReviewerId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @ApprovedStatusId INT;
    SELECT @ApprovedStatusId = StatusId FROM dbo.ExpenseStatus WHERE StatusName = 'Approved';
    
    UPDATE dbo.Expenses
    SET StatusId = @ApprovedStatusId,
        ReviewedBy = @ReviewerId,
        ReviewedAt = SYSUTCDATETIME()
    WHERE ExpenseId = @ExpenseId;
    
    SELECT @@ROWCOUNT AS RowsAffected;
END
GO

-- =============================================
-- REJECT EXPENSE
-- =============================================
CREATE OR ALTER PROCEDURE [dbo].[usp_RejectExpense]
    @ExpenseId INT,
    @ReviewerId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @RejectedStatusId INT;
    SELECT @RejectedStatusId = StatusId FROM dbo.ExpenseStatus WHERE StatusName = 'Rejected';
    
    UPDATE dbo.Expenses
    SET StatusId = @RejectedStatusId,
        ReviewedBy = @ReviewerId,
        ReviewedAt = SYSUTCDATETIME()
    WHERE ExpenseId = @ExpenseId;
    
    SELECT @@ROWCOUNT AS RowsAffected;
END
GO

-- =============================================
-- GET PENDING EXPENSES (for managers to approve)
-- =============================================
CREATE OR ALTER PROCEDURE [dbo].[usp_GetPendingExpenses]
    @CategoryFilter NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        e.ExpenseId,
        e.UserId,
        u.UserName,
        u.Email,
        e.CategoryId,
        c.CategoryName,
        e.StatusId,
        s.StatusName,
        CAST(e.AmountMinor / 100.0 AS DECIMAL(10,2)) AS Amount,
        e.Currency,
        e.ExpenseDate,
        e.Description,
        e.ReceiptFile,
        e.SubmittedAt,
        e.CreatedAt
    FROM dbo.Expenses e
    INNER JOIN dbo.Users u ON e.UserId = u.UserId
    INNER JOIN dbo.ExpenseCategories c ON e.CategoryId = c.CategoryId
    INNER JOIN dbo.ExpenseStatus s ON e.StatusId = s.StatusId
    WHERE s.StatusName = 'Submitted'
      AND (@CategoryFilter IS NULL OR c.CategoryName = @CategoryFilter)
    ORDER BY e.SubmittedAt ASC;
END
GO

-- =============================================
-- GET ALL CATEGORIES
-- =============================================
CREATE OR ALTER PROCEDURE [dbo].[usp_GetCategories]
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT CategoryId, CategoryName, IsActive
    FROM dbo.ExpenseCategories
    WHERE IsActive = 1
    ORDER BY CategoryName;
END
GO

-- =============================================
-- GET ALL STATUSES
-- =============================================
CREATE OR ALTER PROCEDURE [dbo].[usp_GetStatuses]
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT StatusId, StatusName
    FROM dbo.ExpenseStatus
    ORDER BY StatusId;
END
GO

-- =============================================
-- GET ALL USERS
-- =============================================
CREATE OR ALTER PROCEDURE [dbo].[usp_GetUsers]
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        u.UserId,
        u.UserName,
        u.Email,
        u.RoleId,
        r.RoleName,
        u.ManagerId,
        mgr.UserName AS ManagerName,
        u.IsActive,
        u.CreatedAt
    FROM dbo.Users u
    INNER JOIN dbo.Roles r ON u.RoleId = r.RoleId
    LEFT JOIN dbo.Users mgr ON u.ManagerId = mgr.UserId
    WHERE u.IsActive = 1
    ORDER BY u.UserName;
END
GO

-- =============================================
-- GET USER BY ID
-- =============================================
CREATE OR ALTER PROCEDURE [dbo].[usp_GetUserById]
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        u.UserId,
        u.UserName,
        u.Email,
        u.RoleId,
        r.RoleName,
        u.ManagerId,
        mgr.UserName AS ManagerName,
        u.IsActive,
        u.CreatedAt
    FROM dbo.Users u
    INNER JOIN dbo.Roles r ON u.RoleId = r.RoleId
    LEFT JOIN dbo.Users mgr ON u.ManagerId = mgr.UserId
    WHERE u.UserId = @UserId;
END
GO

-- =============================================
-- GET EXPENSE SUMMARY (for dashboard/reports)
-- =============================================
CREATE OR ALTER PROCEDURE [dbo].[usp_GetExpenseSummary]
    @UserId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        s.StatusName,
        COUNT(*) AS ExpenseCount,
        CAST(SUM(e.AmountMinor) / 100.0 AS DECIMAL(10,2)) AS TotalAmount
    FROM dbo.Expenses e
    INNER JOIN dbo.ExpenseStatus s ON e.StatusId = s.StatusId
    WHERE (@UserId IS NULL OR e.UserId = @UserId)
    GROUP BY s.StatusName
    ORDER BY s.StatusName;
END
GO
