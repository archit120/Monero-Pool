



-- -----------------------------------------------------------
-- Entity Designer DDL Script for MySQL Server 4.1 and higher
-- -----------------------------------------------------------
-- Date Created: 05/11/2014 11:39:59
-- Generated from EDMX file: L:\Users\archit\documents\visual studio 2012\Projects\MoneroPool\MoneroPool\MinerModel.edmx
-- Target version: 3.0.0.0
-- --------------------------------------------------


-- --------------------------------------------------
-- Dropping existing FOREIGN KEY constraints
-- NOTE: if the constraint does not exist, an ignorable error will be reported.
-- --------------------------------------------------

--    ALTER TABLE `BlockRewards` DROP CONSTRAINT `FK_BlockBlockReward`;
--    ALTER TABLE `BlockRewards` DROP CONSTRAINT `FK_BlockRewardMiner`;

-- --------------------------------------------------
-- Dropping existing tables
-- --------------------------------------------------
SET foreign_key_checks = 0;
    DROP TABLE IF EXISTS `Miners`;
    DROP TABLE IF EXISTS `Blocks`;
    DROP TABLE IF EXISTS `BlockRewards`;
    DROP TABLE IF EXISTS `ServerInfoes`;
SET foreign_key_checks = 1;

-- --------------------------------------------------
-- Creating all tables
-- --------------------------------------------------

CREATE TABLE `Miners`(
	`Id` int NOT NULL AUTO_INCREMENT UNIQUE, 
	`Address` longtext NOT NULL);

ALTER TABLE `Miners` ADD PRIMARY KEY (Id);




CREATE TABLE `Blocks`(
	`Id` int NOT NULL AUTO_INCREMENT UNIQUE);

ALTER TABLE `Blocks` ADD PRIMARY KEY (Id);




CREATE TABLE `BlockRewards`(
	`Id` int NOT NULL AUTO_INCREMENT UNIQUE, 
	`Shares` bigint NOT NULL, 
	`Value` double NOT NULL, 
	`BlockId` int NOT NULL, 
	`Miner_Id` int NOT NULL);

ALTER TABLE `BlockRewards` ADD PRIMARY KEY (Id);




CREATE TABLE `ServerInfoes`(
	`Id` int NOT NULL AUTO_INCREMENT UNIQUE, 
	`LastPaidBlock` int NOT NULL);

ALTER TABLE `ServerInfoes` ADD PRIMARY KEY (Id);






-- --------------------------------------------------
-- Creating all FOREIGN KEY constraints
-- --------------------------------------------------

-- Creating foreign key on `BlockId` in table 'BlockRewards'

ALTER TABLE `BlockRewards`
ADD CONSTRAINT `FK_BlockBlockReward`
    FOREIGN KEY (`BlockId`)
    REFERENCES `Blocks`
        (`Id`)
    ON DELETE NO ACTION ON UPDATE NO ACTION;

-- Creating non-clustered index for FOREIGN KEY 'FK_BlockBlockReward'

CREATE INDEX `IX_FK_BlockBlockReward` 
    ON `BlockRewards`
    (`BlockId`);

-- Creating foreign key on `Miner_Id` in table 'BlockRewards'

ALTER TABLE `BlockRewards`
ADD CONSTRAINT `FK_BlockRewardMiner`
    FOREIGN KEY (`Miner_Id`)
    REFERENCES `Miners`
        (`Id`)
    ON DELETE NO ACTION ON UPDATE NO ACTION;

-- Creating non-clustered index for FOREIGN KEY 'FK_BlockRewardMiner'

CREATE INDEX `IX_FK_BlockRewardMiner` 
    ON `BlockRewards`
    (`Miner_Id`);

-- --------------------------------------------------
-- Script has ended
-- --------------------------------------------------
