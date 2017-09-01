SET NOCOUNT ON
/*
�f�t Agent_LCU_forSMD �i�H�ֳt�U�Ǩ���wFTP


select * from SMD_DEVICE where ID like 'PC4%'
*/


DECLARE 
	@DEVICE_AREA	varchar(50)='A4WS1'	--���Χ�
	,@DEVICE_ID		varchar(50)='LCU1'	--���Χ�
	,@HID			varchar(50)='\PXG\SMD\PC4\LCU700\'
	,@D_DATE		varchar(10)='2017-08-31' 
	,@CALLING_NUM	varchar(10)='311381'


--�۰ʨ��o���OrderNo
DECLARE @OrderNo varchar(50)=''
SELECT @OrderNo=ISNULL(OrderNo,'')
FROM [DDI].[dbo].[DDI_WORKSPACE_STATUS]
WHERE WORKSPACE=@DEVICE_AREA
	AND DEVICE_ID=@DEVICE_ID
	AND [WORKSTATUS] in(0,2)

IF(@OrderNo='')
BEGIN
	RAISERROR ('�妸���s�b',16,1)
END

TRUNCATE TABLE [dbo].[ob.DDI_UD_LCU_TERAOKA_LCU700_V2_HST0023]

INSERT INTO [ob.DDI_UD_LCU_TERAOKA_LCU700_V2_HST0023]
select 
	Field01='CS'
	,Field02=BATCH_ID		--�Z�O
	,Field03=PD.CALLING_NUM	--�I�X�X
	,Field04=STR_ID			--���Q
	,Field05=convert(int,convert(decimal(9,2),PD.PRICE_G))	--���
	,Field06=convert(int,convert(decimal(9,2),PD.COST))		--����
	,Field07=convert(int,convert(decimal(9,2),PD.PRICE_P))	--���
	,Field08=QTY
	,Field09='DD'
	,Field10=CASE ISNULL(SAV.SAVING_TYPE, IT.SAVING_TYPE) WHEN 11 THEN 1 WHEN 12 THEN 2 ELSE 'DDDD' END	--AD_TEXT
	,Field11='DDDD'
	,Field12=PD.PRICE_TYPE			--�p���Ҧ�
	,Field13=PD.PROMO_PRICE_FLAG	--�S��]�w
	,Field14='0'
	,Field15=ISNULL(EL.VALUE,'0')	--TwoPage
	,Field16='0'
	,Field17='03'
	,Field18='20'+PD.CALLING_NUM+'0000'
	,Field19=''
	,Field20=PD.SELL_BY_DAY			--���Ĥ�
	,Field21=CASE WHEN IT.PC_DATE_PRINT_FLAG = '1' THEN '0' ELSE '1' END
	,Field22=''
	,Field23=''
	,Field24=CASE WHEN IT.SB_DATE_PRINT_FLAG = '1' THEN '0' ELSE '1' END 
	,Field25=''
	,Field26=''
	,Field27=''	--���Ĵ���
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
	,@DEVICE_AREA
	,@DEVICE_ID
	,TXTSEQ=SEQ_ID
	,[Status]=0
FROM [192.168.100.65].SMD.dbo.SMD_DCS_LCU_PD PD
LEFT JOIN [192.168.100.65].SMD.dbo.SMD_ITEM IT ON PD.ITEM_ID = IT.ID AND IT.COM_HID = '\PXG\SMD\'
LEFT JOIN [192.168.100.65].SMD.dbo.SMD_INTERFACE_SAVEING_TYPE SAV ON IT.ID = SAV.ID
LEFT JOIN [192.168.100.65].SMD.dbo.smd_EL_ITEM EL ON EL.[TYPE]=4 AND EL.VALUE>0 AND PD.CALLING_NUM=EL.CN
WHERE PD.HID = @HID
	AND PD.D_DATE = @D_DATE
	AND PD.CALLING_NUM= @CALLING_NUM
	-- AND PD.LCU_STATUS = 99
ORDER BY PD.SEQ_ID

print '�w�s�W '+convert(varchar,@@ROWCOUNT)+' �����'