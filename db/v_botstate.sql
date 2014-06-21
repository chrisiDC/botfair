

CREATE VIEW [dbo].[V_BotState]
AS

select top 1 * from BotState where dateValid = (select MAX(dateValid)from BotState)
GO

