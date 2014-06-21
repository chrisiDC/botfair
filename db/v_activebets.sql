




CREATE VIEW [dbo].[V_ActiveBets]
as

select m.id as marketId,m.menuPath,m.marketStatus,m.eventDate, b.* from [bets] b
inner join Markets m on b.fk_market = m.id
where b.sizePosted > 0 and m.marketStatus = 'active'







