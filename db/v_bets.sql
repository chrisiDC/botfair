CREATE VIEW [dbo].[V_Bets]
as

select m.id as marketId,m.name,m.eventDate,s.name as selectionName, b.* from [bets] b
inner join Markets m on b.fk_market = m.id
inner join Selections s on s.selectionId = b.fk_selection
where b.datePosted > '1970-01-01 00:00:00.000'