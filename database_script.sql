IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
CREATE TABLE [Caretakers] (
    [CaretakerId] int NOT NULL IDENTITY,
    [Name] nvarchar(max) NOT NULL,
    [RestaurantName] nvarchar(max) NOT NULL,
    [Address] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Caretakers] PRIMARY KEY ([CaretakerId])
);

CREATE TABLE [Users] (
    [UserId] int NOT NULL IDENTITY,
    [Name] nvarchar(max) NOT NULL,
    [Email] nvarchar(max) NOT NULL,
    [Password] nvarchar(max) NOT NULL,
    [Role] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY ([UserId])
);

CREATE TABLE [MenuItems] (
    [MenuItemId] int NOT NULL IDENTITY,
    [Name] nvarchar(max) NOT NULL,
    [Price] decimal(18,2) NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    [CaretakerId] int NULL,
    CONSTRAINT [PK_MenuItems] PRIMARY KEY ([MenuItemId]),
    CONSTRAINT [FK_MenuItems_Caretakers_CaretakerId] FOREIGN KEY ([CaretakerId]) REFERENCES [Caretakers] ([CaretakerId])
);

CREATE TABLE [Orders] (
    [OrderId] int NOT NULL IDENTITY,
    [OrderDate] datetime2 NOT NULL,
    [TotalPrice] decimal(18,2) NOT NULL,
    [UserId] int NOT NULL,
    CONSTRAINT [PK_Orders] PRIMARY KEY ([OrderId]),
    CONSTRAINT [FK_Orders_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([UserId]) ON DELETE CASCADE
);

CREATE TABLE [CustomizationOptions] (
    [CustomizationOptionId] int NOT NULL IDENTITY,
    [Name] nvarchar(max) NOT NULL,
    [ExtraPrice] decimal(18,2) NOT NULL,
    [MenuItemId] int NOT NULL,
    CONSTRAINT [PK_CustomizationOptions] PRIMARY KEY ([CustomizationOptionId]),
    CONSTRAINT [FK_CustomizationOptions_MenuItems_MenuItemId] FOREIGN KEY ([MenuItemId]) REFERENCES [MenuItems] ([MenuItemId]) ON DELETE CASCADE
);

CREATE TABLE [OrderItems] (
    [OrderItemId] int NOT NULL IDENTITY,
    [Quantity] int NOT NULL,
    [UnitPrice] decimal(18,2) NOT NULL,
    [OrderId] int NOT NULL,
    [MenuItemId] int NOT NULL,
    CONSTRAINT [PK_OrderItems] PRIMARY KEY ([OrderItemId]),
    CONSTRAINT [FK_OrderItems_MenuItems_MenuItemId] FOREIGN KEY ([MenuItemId]) REFERENCES [MenuItems] ([MenuItemId]) ON DELETE CASCADE,
    CONSTRAINT [FK_OrderItems_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([OrderId]) ON DELETE CASCADE
);

CREATE INDEX [IX_CustomizationOptions_MenuItemId] ON [CustomizationOptions] ([MenuItemId]);

CREATE INDEX [IX_MenuItems_CaretakerId] ON [MenuItems] ([CaretakerId]);

CREATE INDEX [IX_OrderItems_MenuItemId] ON [OrderItems] ([MenuItemId]);

CREATE INDEX [IX_OrderItems_OrderId] ON [OrderItems] ([OrderId]);

CREATE INDEX [IX_Orders_UserId] ON [Orders] ([UserId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260424125344_InitialCreate', N'10.0.5');

COMMIT;
GO

BEGIN TRANSACTION;
ALTER TABLE [Orders] ADD [Status] nvarchar(max) NOT NULL DEFAULT N'';

ALTER TABLE [OrderItems] ADD [CustomizationName] nvarchar(max) NOT NULL DEFAULT N'';

ALTER TABLE [OrderItems] ADD [CustomizationPrice] decimal(18,2) NOT NULL DEFAULT 0.0;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260424131655_UpdateOrderModelsForWeek3', N'10.0.5');

COMMIT;
GO

BEGIN TRANSACTION;
ALTER TABLE [MenuItems] DROP CONSTRAINT [FK_MenuItems_Caretakers_CaretakerId];

ALTER TABLE [Users] ADD [Latitude] float NULL;

ALTER TABLE [Users] ADD [Longitude] float NULL;

ALTER TABLE [Caretakers] ADD [Latitude] float NULL;

ALTER TABLE [Caretakers] ADD [Longitude] float NULL;

CREATE TABLE [CaretakerRatings] (
    [CaretakerRatingId] int NOT NULL IDENTITY,
    [RatingValue] int NOT NULL,
    [Comment] nvarchar(max) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UserId] int NOT NULL,
    [OrderId] int NOT NULL,
    [CaretakerId] int NOT NULL,
    CONSTRAINT [PK_CaretakerRatings] PRIMARY KEY ([CaretakerRatingId]),
    CONSTRAINT [FK_CaretakerRatings_Caretakers_CaretakerId] FOREIGN KEY ([CaretakerId]) REFERENCES [Caretakers] ([CaretakerId]),
    CONSTRAINT [FK_CaretakerRatings_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([OrderId]),
    CONSTRAINT [FK_CaretakerRatings_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([UserId])
);

CREATE TABLE [MenuItemRatings] (
    [MenuItemRatingId] int NOT NULL IDENTITY,
    [RatingValue] int NOT NULL,
    [Comment] nvarchar(max) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UserId] int NOT NULL,
    [OrderId] int NOT NULL,
    [MenuItemId] int NOT NULL,
    CONSTRAINT [PK_MenuItemRatings] PRIMARY KEY ([MenuItemRatingId]),
    CONSTRAINT [FK_MenuItemRatings_MenuItems_MenuItemId] FOREIGN KEY ([MenuItemId]) REFERENCES [MenuItems] ([MenuItemId]),
    CONSTRAINT [FK_MenuItemRatings_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([OrderId]),
    CONSTRAINT [FK_MenuItemRatings_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([UserId])
);

CREATE TABLE [SystemLogs] (
    [SystemLogId] int NOT NULL IDENTITY,
    [EventType] nvarchar(max) NOT NULL,
    [Message] nvarchar(max) NOT NULL,
    [UserEmail] nvarchar(max) NULL,
    [UserRole] nvarchar(max) NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_SystemLogs] PRIMARY KEY ([SystemLogId])
);

CREATE INDEX [IX_CaretakerRatings_CaretakerId] ON [CaretakerRatings] ([CaretakerId]);

CREATE INDEX [IX_CaretakerRatings_OrderId] ON [CaretakerRatings] ([OrderId]);

CREATE INDEX [IX_CaretakerRatings_UserId] ON [CaretakerRatings] ([UserId]);

CREATE INDEX [IX_MenuItemRatings_MenuItemId] ON [MenuItemRatings] ([MenuItemId]);

CREATE INDEX [IX_MenuItemRatings_OrderId] ON [MenuItemRatings] ([OrderId]);

CREATE INDEX [IX_MenuItemRatings_UserId] ON [MenuItemRatings] ([UserId]);

ALTER TABLE [MenuItems] ADD CONSTRAINT [FK_MenuItems_Caretakers_CaretakerId] FOREIGN KEY ([CaretakerId]) REFERENCES [Caretakers] ([CaretakerId]) ON DELETE SET NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260503125721_Week4InitialModels', N'10.0.5');

COMMIT;
GO

BEGIN TRANSACTION;
ALTER TABLE [Caretakers] ADD [UserId] int NULL;

CREATE UNIQUE INDEX [IX_Caretakers_UserId] ON [Caretakers] ([UserId]) WHERE [UserId] IS NOT NULL;

ALTER TABLE [Caretakers] ADD CONSTRAINT [FK_Caretakers_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([UserId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260503140809_ConnectCaretakerToUser', N'10.0.5');

COMMIT;
GO

BEGIN TRANSACTION;
ALTER TABLE [Users] ADD [AddressDetail] nvarchar(max) NOT NULL DEFAULT N'';

ALTER TABLE [Users] ADD [City] nvarchar(max) NOT NULL DEFAULT N'';

ALTER TABLE [Users] ADD [District] nvarchar(max) NOT NULL DEFAULT N'';

ALTER TABLE [Users] ADD [Neighborhood] nvarchar(max) NOT NULL DEFAULT N'';

ALTER TABLE [Users] ADD [Street] nvarchar(max) NOT NULL DEFAULT N'';

ALTER TABLE [Caretakers] ADD [AddressDetail] nvarchar(max) NOT NULL DEFAULT N'';

ALTER TABLE [Caretakers] ADD [City] nvarchar(max) NOT NULL DEFAULT N'';

ALTER TABLE [Caretakers] ADD [District] nvarchar(max) NOT NULL DEFAULT N'';

ALTER TABLE [Caretakers] ADD [Neighborhood] nvarchar(max) NOT NULL DEFAULT N'';

ALTER TABLE [Caretakers] ADD [Street] nvarchar(max) NOT NULL DEFAULT N'';

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260504164213_AddAddressFieldsForGoogleMaps', N'10.0.5');

COMMIT;
GO

BEGIN TRANSACTION;
ALTER TABLE [MenuItems] ADD [ImageUrl] nvarchar(max) NOT NULL DEFAULT N'';

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260504170030_AddMenuItemImageUrl', N'10.0.5');

COMMIT;
GO

BEGIN TRANSACTION;
ALTER TABLE [MenuItems] ADD [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260504170740_AddSoftDeleteToMenuItems', N'10.0.5');

COMMIT;
GO

BEGIN TRANSACTION;
ALTER TABLE [Users] ADD [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit);

ALTER TABLE [Caretakers] ADD [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260515134428_AddSoftDeleteToUsersAndCaretakers', N'10.0.5');

COMMIT;
GO

BEGIN TRANSACTION;
ALTER TABLE [MenuItems] ADD [Category] nvarchar(max) NOT NULL DEFAULT N'';

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260515143153_AddCategoryToMenuItemsForCatering', N'10.0.5');

COMMIT;
GO

BEGIN TRANSACTION;
INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260515144553_AddMenuItemCategoryColumn', N'10.0.5');

COMMIT;
GO

