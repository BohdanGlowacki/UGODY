using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UGODY.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Defensive migration - check if tables exist before creating using SQL
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Configurations')
                BEGIN
                    CREATE TABLE [Configurations] (
                        [Id] int NOT NULL IDENTITY,
                        [Key] nvarchar(100) NOT NULL,
                        [Value] nvarchar(2000) NOT NULL,
                        [UpdatedDate] datetime2 NOT NULL,
                        CONSTRAINT [PK_Configurations] PRIMARY KEY ([Id])
                    );
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PdfFiles')
                BEGIN
                    CREATE TABLE [PdfFiles] (
                        [Id] int NOT NULL IDENTITY,
                        [FileName] nvarchar(500) NOT NULL,
                        [FilePath] nvarchar(1000) NOT NULL,
                        [FileContent] varbinary(max) NOT NULL,
                        [FileSize] bigint NOT NULL,
                        [Hash] nvarchar(64) NOT NULL,
                        [CreatedDate] datetime2 NOT NULL,
                        [LastModifiedDate] datetime2 NOT NULL,
                        CONSTRAINT [PK_PdfFiles] PRIMARY KEY ([Id])
                    );
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'OcrResults')
                BEGIN
                    CREATE TABLE [OcrResults] (
                        [Id] int NOT NULL IDENTITY,
                        [PdfFileId] int NOT NULL,
                        [ExtractedText] nvarchar(max) NOT NULL,
                        [Confidence] float NULL,
                        [ProcessedDate] datetime2 NOT NULL,
                        [ProcessingStatus] int NOT NULL,
                        [ErrorMessage] nvarchar(max) NULL,
                        CONSTRAINT [PK_OcrResults] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_OcrResults_PdfFiles_PdfFileId] FOREIGN KEY ([PdfFileId]) REFERENCES [PdfFiles] ([Id]) ON DELETE CASCADE
                    );
                END
            ");

            // Create indexes defensively
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Configurations_Key' AND object_id = OBJECT_ID('Configurations'))
                BEGIN
                    CREATE UNIQUE INDEX [IX_Configurations_Key] ON [Configurations] ([Key]);
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_OcrResults_PdfFileId' AND object_id = OBJECT_ID('OcrResults'))
                BEGIN
                    CREATE INDEX [IX_OcrResults_PdfFileId] ON [OcrResults] ([PdfFileId]);
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_PdfFiles_FileName' AND object_id = OBJECT_ID('PdfFiles'))
                BEGIN
                    CREATE INDEX [IX_PdfFiles_FileName] ON [PdfFiles] ([FileName]);
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_PdfFiles_Hash' AND object_id = OBJECT_ID('PdfFiles'))
                BEGIN
                    CREATE UNIQUE INDEX [IX_PdfFiles_Hash] ON [PdfFiles] ([Hash]);
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Defensive migration - check if tables exist before dropping
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.tables WHERE name = 'OcrResults')
                BEGIN
                    DROP TABLE [OcrResults];
                END
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Configurations')
                BEGIN
                    DROP TABLE [Configurations];
                END
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.tables WHERE name = 'PdfFiles')
                BEGIN
                    DROP TABLE [PdfFiles];
                END
            ");
        }
    }
}
