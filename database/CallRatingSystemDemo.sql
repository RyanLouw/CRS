USE [CallRatingSystemDemo]
GO
/****** Object:  Table [dbo].[CallRating]    Script Date: 2026/11/13 12:12:20 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[CallRating](
	[CallRatingID] [bigint] IDENTITY(1,1) NOT NULL,
	[UserID] [uniqueidentifier] NULL,
	[CaptureDate] [datetime] NULL,
	[Ext] [nvarchar](max) NULL,
	[CallDate] [nvarchar](max) NULL,
	[CallTime] [nvarchar](max) NULL,
	[NumberPhoned] [nvarchar](max) NULL,
	[Problems] [nvarchar](max) NULL,
	[OtherProblem] [nvarchar](max) NULL,
	[CallDateTime] [datetime] NULL,
	[WiFi] [bit] NULL,
	[LAN] [bit] NULL,
	[ClientIP] [nvarchar](max) NULL,
	[HostIP] [nvarchar](max) NULL,
	[call_id] [varchar](max) NULL,
	[ITUpdateByUserID] [uniqueidentifier] NULL,
	[ITUpdateDate] [datetime] NULL,
	[HelpDeskStatusID] [bigint] NULL,
	[ITNote] [nvarchar](max) NULL,
	[HelpDeskReasonID] [bigint] NULL,
	[PharmacyID] [bigint] NULL,
 CONSTRAINT [PK_CallRating] PRIMARY KEY CLUSTERED
(
	[CallRatingID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Lookup_HelpDeskReason]    Script Date: 2026/11/13 12:12:20 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Lookup_HelpDeskReason](
	[HelpDeskReasonID] [bigint] IDENTITY(1,1) NOT NULL,
	[HelpDeskReason] [nvarchar](max) NULL,
	[Active] [bit] NULL,
 CONSTRAINT [PK_Lookup_HelpDeskReason] PRIMARY KEY CLUSTERED
(
	[HelpDeskReasonID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Lookup_HelpDeskStatus]    Script Date: 2026/11/13 12:12:20 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Lookup_HelpDeskStatus](
	[HelpDeskStatusID] [bigint] IDENTITY(1,1) NOT NULL,
	[HelpDeskStatus] [nvarchar](max) NULL,
	[Active] [bit] NULL,
 CONSTRAINT [PK_Lookup_HelpDeskStatus] PRIMARY KEY CLUSTERED
(
	[HelpDeskStatusID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Lookup_Pharmacy]    Script Date: 2026/11/13 12:12:20 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Lookup_Pharmacy](
	[PharmacyID] [bigint] IDENTITY(1,1) NOT NULL,
	[Pharmacy] [nvarchar](max) NULL,
	[Active] [bit] NULL,
 CONSTRAINT [PK_Lookup_Pharmacy] PRIMARY KEY CLUSTERED
(
	[PharmacyID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Lookup_Problems]    Script Date: 2026/11/13 12:12:20 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Lookup_Problems](
	[ProblemsID] [bigint] IDENTITY(1,1) NOT NULL,
	[Problems] [nvarchar](max) NULL,
	[Active] [bit] NULL,
 CONSTRAINT [PK_Lookup_Problems] PRIMARY KEY CLUSTERED
(
	[ProblemsID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  StoredProcedure [dbo].[CPS_ReportCallRatings]    Script Date: 2026/11/13 12:12:20 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
Create procedure [dbo].[CPS_ReportCallRatings]

as

SELECT
u.DisplayName
	  ,u.GivenName
      ,cr.[CallRatingID]
      ,cr.[UserID]
      ,cr.[CaptureDate]
      ,cr.[Ext]
      ,cr.[CallDate] [Call Date]
      ,cr.[CallTime] [Call Time]
      ,cr.[NumberPhoned] [Number Dialed]
      ,cr.[Problems]
      ,cr.[OtherProblem]
      ,cr.[CallDateTime]

      ,cr.[ClientIP]
      ,cr.[HostIP]
      ,cr.[call_id]
	  ,lhds.HelpDeskStatus
	  ,cr.ITNote
	  ,lhdr.HelpDeskReason
	  ,uIT.DisplayName ITUpdateByUser
	  ,cr.ITUpdateDate
	  ,ph.Pharmacy
  FROM [dbo].[CallRating] cr
  left join ADUsers.dbo.ADUser u on u.ID = cr.UserID
  left join ADUsers.dbo.ADUser uIT on uIT.ID = cr.ITUpdateByUserID
  left join Lookup_HelpDeskStatus lhds on lhds.HelpDeskStatusID = cr.HelpDeskStatusID
  left join Lookup_HelpDeskReason lhdr on lhdr.HelpDeskReasonID = cr.HelpDeskReasonID
  left join Lookup_Pharmacy ph on ph.PharmacyID = cr.PharmacyID
  order by CaptureDate desc
GO
/****** Object:  StoredProcedure [dbo].[CSP_AddCallRating]    Script Date: 2026/11/13 12:12:20 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE procedure [dbo].[CSP_AddCallRating]


@UserID uniqueidentifier = null,
@CaptureDate datetime = null,
@Ext nvarchar(max) = null,
@CallDate nvarchar(max) = null,
@CallTime nvarchar(max) = null,
@NumberPhoned nvarchar(max) = null,
@Problems nvarchar(max) = null,
@OtherProblem nvarchar(max) = null,
@CallDateTime datetime = null,
@WiFi bit = null,
@LAN bit = null,
@ClientIP nvarchar(max) = null,
@HostIP nvarchar(max) = null,
@call_id nvarchar(MAX) = null,
@PharmacyID bigint = null

as




INSERT INTO [dbo].[CallRating]
           ([UserID]
           ,[CaptureDate]
           ,[Ext]
           ,[CallDate]
           ,[CallTime]
           ,[NumberPhoned]
		   ,[Problems]
           ,[OtherProblem]
           ,[CallDateTime]
           ,[WiFi]
           ,[LAN]
           ,[ClientIP]
		   ,[HostIP]
		   ,[call_id]
		   ,[HelpDeskStatusID]
		   ,[PharmacyID]
		   )
     VALUES
           (@UserID
           ,@CaptureDate
           ,@Ext
           ,@CallDate
           ,@CallTime
           ,@NumberPhoned
		   ,@Problems
           ,@OtherProblem
           ,@CallDateTime
           ,@WiFi
           ,@LAN
           ,@ClientIP
		   ,@HostIP
		   ,@call_id
		   ,1 --Open
		   ,@PharmacyID
		   )
GO
/****** Object:  StoredProcedure [dbo].[CSP_GetCallRating]    Script Date: 2026/11/13 12:12:20 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE procedure [dbo].[CSP_GetCallRating]

@HelpDeskStatusFilter bigint = null

as
SELECT
u.DisplayName
	  ,u.GivenName
      ,cr.[CallRatingID]
      ,cr.[UserID]
      ,cr.[CaptureDate]
      ,cr.[Ext]
      ,cr.[CallDate] [Call Date]
      ,cr.[CallTime] [Call Time]
      ,cr.[NumberPhoned] [Number Dialed]
      ,cr.[Problems]
      ,cr.[OtherProblem]
      ,cr.[CallDateTime]
      ,cr.[WiFi]
      ,cr.[LAN]
      ,cr.[ClientIP]
      ,cr.[HostIP]
      ,cr.[call_id]
	  ,cr.HelpDeskStatusID
	  ,lhds.HelpDeskStatus
	  ,cr.ITNote
	  ,cr.HelpDeskReasonID
	  ,lhdr.HelpDeskReason
	  ,uIT.DisplayName ITUpdateByUser
	  ,cr.ITUpdateDate
	  ,cr.PharmacyID
	  ,ph.Pharmacy
  FROM [dbo].[CallRating] cr
  left join ADUsers.dbo.ADUser u on u.ID = cr.UserID
  left join ADUsers.dbo.ADUser uIT on uIT.ID = cr.ITUpdateByUserID
  left join Lookup_HelpDeskStatus lhds on lhds.HelpDeskStatusID = cr.HelpDeskStatusID
  left join Lookup_HelpDeskReason lhdr on lhdr.HelpDeskReasonID = cr.HelpDeskReasonID
  left join Lookup_Pharmacy ph on ph.PharmacyID = cr.PharmacyID
  where cr.HelpDeskStatusID = ISNULL(@HelpDeskStatusFilter, cr.HelpDeskStatusID)
  order by CaptureDate desc
GO
/****** Object:  StoredProcedure [dbo].[CSP_GetCallRatingPage]    Script Date: 2026/11/13 12:12:20 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE procedure [dbo].[CSP_GetCallRatingPage]

@HelpDeskStatusFilter bigint = null,
@PageNum bigint = null

as
SELECT top 5
u.DisplayName
	  ,u.GivenName
      ,cr.[CallRatingID]
      ,cr.[UserID]
      ,cr.[CaptureDate]
      ,cr.[Ext]
      ,cr.[CallDate] [Call Date]
      ,cr.[CallTime] [Call Time]
      ,cr.[NumberPhoned] [Number Dialed]
      ,cr.[Problems]
      ,cr.[OtherProblem]
      ,cr.[CallDateTime]
      ,cr.[WiFi]
      ,cr.[LAN]
      ,cr.[ClientIP]
      ,cr.[HostIP]
      ,cr.[call_id]
	  ,cr.HelpDeskStatusID
	  ,lhds.HelpDeskStatus
	  ,cr.ITNote
	  ,cr.HelpDeskReasonID
	  ,lhdr.HelpDeskReason
	  ,uIT.DisplayName ITUpdateByUser
	  ,cr.ITUpdateDate
	  ,cr.PharmacyID
	  ,ph.Pharmacy
	  ,uCM.Mail
  FROM [dbo].[CallRating] cr
  left join ADUsers.dbo.ADUser u on u.ID = cr.UserID
  left join ADUsers.dbo.ADUser uIT on uIT.ID = cr.ITUpdateByUserID
  left join Lookup_HelpDeskStatus lhds on lhds.HelpDeskStatusID = cr.HelpDeskStatusID
  left join Lookup_HelpDeskReason lhdr on lhdr.HelpDeskReasonID = cr.HelpDeskReasonID
  left join Lookup_Pharmacy ph on ph.PharmacyID = cr.PharmacyID
  left join ADUsers.dbo.ADUser uCM on uCM.Extension = cr.Ext
  where cr.HelpDeskStatusID = ISNULL(@HelpDeskStatusFilter, cr.HelpDeskStatusID)
  and [CallRatingID] <= ((select top 1 crm.[CallRatingID] FROM [dbo].[CallRating] crm order by crm.[CallRatingID] desc) - (5 * @PageNum))
  order by CaptureDate desc
GO
/****** Object:  StoredProcedure [dbo].[CSP_GetDropdownList]    Script Date: 2026/11/13 12:12:20 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[CSP_GetDropdownList]

	@DropdownType nvarchar(50),
	@PatientID bigint =  null
AS
BEGIN


	SET NOCOUNT ON;



IF @DropdownType = 'HelpDeskReason'
BEGIN
SELECT [HelpDeskReasonID]
      ,[HelpDeskReason]
  FROM [dbo].Lookup_HelpDeskReason
  where Active = 1
END

IF @DropdownType = 'HelpDeskStatus'
BEGIN
SELECT [HelpDeskStatusID]
      ,[HelpDeskStatus]
  FROM [dbo].Lookup_HelpDeskStatus
  where Active = 1
END

IF @DropdownType = 'Problems'
BEGIN
SELECT [ProblemsID]
      ,[Problems]
  FROM [dbo].Lookup_Problems
  where Active = 1
END

IF @DropdownType = 'Pharmacy'
BEGIN
SELECT [PharmacyID]
      ,[Pharmacy]
  FROM [dbo].Lookup_Pharmacy
  where Active = 1
END

END
GO
/****** Object:  StoredProcedure [dbo].[CSP_GetProblems]    Script Date: 2026/11/13 12:12:20 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
create procedure [dbo].[CSP_GetProblems]

as


SELECT [ProblemsID]
      ,[Problems]
  FROM [dbo].[Lookup_Problems]
  where Active = 1
GO
/****** Object:  StoredProcedure [dbo].[CSP_GetRatingUserEmail]    Script Date: 2026/11/13 12:12:20 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE procedure [dbo].[CSP_GetRatingUserEmail]

@Ext nvarchar(50)

as

select u.Mail
from ADUsers.dbo.ADUser u
where u.Extension = @Ext
GO
/****** Object:  StoredProcedure [dbo].[CSP_GetUserName]    Script Date: 2026/11/13 12:12:20 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
Create procedure [dbo].[CSP_GetUserName]

@UserID uniqueidentifier

as

select u.DisplayName
from ADUsers.dbo.ADUser u
where u.ID = @UserID
GO
/****** Object:  StoredProcedure [dbo].[CSP_ITUpdate]    Script Date: 2026/11/13 12:12:20 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE procedure [dbo].[CSP_ITUpdate]

@CallRatingID bigint,
@ITUpdateByUserID uniqueidentifier,
@ITUpdateDate datetime,
@HelpDeskStatusID bigint = null,
@HelpDeskReasonID bigint = null,
@ITNote nvarchar(max) = null

as

UPDATE [dbo].[CallRating]
   SET [ITUpdateByUserID] = @ITUpdateByUserID
      ,[ITUpdateDate] = @ITUpdateDate
      ,[HelpDeskStatusID] = @HelpDeskStatusID
	  ,[HelpDeskReasonID] = @HelpDeskReasonID
      ,[ITNote] = @ITNote
 WHERE CallRatingID = @CallRatingID
GO
