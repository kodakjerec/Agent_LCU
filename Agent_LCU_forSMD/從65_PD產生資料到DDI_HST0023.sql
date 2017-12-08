USE DDI_UNDER
GO

SET NOCOUNT ON
/*
搭配 Agent_LCU_forSMD 可以快速下傳到指定FTP


select * from SMD_DEVICE where ID like 'PC4%'
*/


DECLARE 
	@DEVICE_AREA	varchar(50)='PC2'	--不用改
	,@DEVICE_ID		varchar(50)='LCU1'	--不用改
	,@ID			varchar(50)='PC2LCUDAS'
	,@SMD_JOBID		varchar(10)='17120502' 
	,@CALLING_NUM	varchar(10)=''


--取得HID
DECLARE @HID varchar(50)=''
select @HID=HID
from [192.168.100.65].SMD.dbo.SMD_DEVICE with(nolock)
where ID=@ID

--自動取得當期OrderNo
DECLARE @OrderNo varchar(50)=''
SELECT @OrderNo=MAX(ISNULL(OrderNo,''))
FROM [DDI].[dbo].[DDI_WORKSPACE_STATUS]
WHERE  DEVICE_ID=@DEVICE_ID
	AND [WORKSTATUS] in(0,2)

IF(@OrderNo='')
BEGIN
	RAISERROR ('批次不存在',16,1)
END

DELETE FROM [ob.DDI_UD_LCU_TERAOKA_LCU700_V2_HST0023]
WHERE (Field03=@CALLING_NUM or ''=@CALLING_NUM)

--INSERT INTO [ob.DDI_UD_LCU_TERAOKA_LCU700_V2_HST0023]
select 
	Field01='CS'
	,Field02=BATCH_ID		--班別
	,Field03=PD.CALLING_NUM	--呼出碼
	,Field04=STR_ID			--店鋪
	,Field05=convert(int,convert(decimal(9,2),PD.PRICE_G))	--售價
	,Field06=convert(int,convert(decimal(9,2),PD.COST))		--成本
	,Field07=convert(int,convert(decimal(9,2),PD.PRICE_P))	--售價
	,Field08=QTY
	,Field09='DD'
	,Field10=CASE ISNULL(SAV.SAVING_TYPE, IT.SAVING_TYPE) WHEN 11 THEN '1' WHEN 12 THEN '2' ELSE 'DDDD' END	--AD_TEXT
	,Field11='DDDD'
	,Field12=PD.PRICE_TYPE			--計價模式
	,Field13=PD.PROMO_PRICE_FLAG	--特賣設定
	,Field14='0'
	,Field15=ISNULL(EL.VALUE,'0')	--TwoPage
	,Field16='0'
	,Field17='03'
	,Field18='20'+PD.CALLING_NUM+'0000'
	,Field19=''
	,Field20=PD.SELL_BY_DAY			--有效日
	,Field21=CASE WHEN IT.PC_DATE_PRINT_FLAG = '1' THEN '0' ELSE '1' END
	,Field22=''
	,Field23=''
	,Field24=CASE WHEN IT.SB_DATE_PRINT_FLAG = '1' THEN '0' ELSE '1' END 
	,Field25=''
	,Field26=''
	,Field27=''	--有效期間
	,Field28=''
	,Field29=''
	,Field30=''	--convert(int,convert(decimal(9,2),a.b.UNIT_WEIGHT))
	,Field31='L'
	,Field32='L'
	,Field33='L'
	,Field34='L'
	,Field35='L'
	,Field36='00000'
	,Field37='0'
	,@OrderNo
	,@ID
	,@DEVICE_ID
	,TXTSEQ=SEQ_ID
	,[Status]=0
FROM [192.168.100.65].SMD.dbo.SMD_DCS_LCU_PD PD
LEFT JOIN [192.168.100.65].SMD.dbo.SMD_ITEM IT ON PD.ITEM_ID = IT.ID AND IT.COM_HID = '\PXG\SMD\'
LEFT JOIN [192.168.100.65].SMD.dbo.SMD_INTERFACE_SAVEING_TYPE SAV ON IT.ID = SAV.ID
LEFT JOIN [192.168.100.65].SMD.dbo.smd_EL_ITEM EL ON EL.[TYPE]=4 AND EL.VALUE>0 AND PD.CALLING_NUM=EL.CN
WHERE PD.HID = @HID
	AND PD.ID = @SMD_JOBID
	AND (PD.CALLING_NUM= @CALLING_NUM or ''=@CALLING_NUM)
	-- AND PD.LCU_STATUS = 99
ORDER BY PD.SEQ_ID

print '已新增 '+convert(varchar,@@ROWCOUNT)+' 筆資料'


/*
/*實績比對*/
drop table #tmp1
drop table #tmp2

select CALLING_NUM, STR_ID, Assign_Qty=SUM(QTY)
INTO #tmp1
from SMD_DCS_LCU_PD a with(nolock)
where ID='17110201'
	and HID like '\PXG\SMD\PC2\LCU700\'
GROUP BY CALLING_NUM, STR_ID
ORDER BY CALLING_NUM, STR_ID

select FIeld03,Field05,ACT_qty=SUM(Field10)
Into #tmp2
from LCU_0301 with(nolock)
where Device_ID='PC2LCUDAS'
group by Field03,Field05
order by Field03,Field05


select a.*,ACT_qty=ISNULL(b.ACT_qty,0),Last_Qty=a.Assign_Qty-ISNULL(b.ACT_qty,0)
from #tmp1 a
left join #tmp2 b
on a.CALLING_NUM=b.Field03
	and a.STR_ID=b.Field05

*/