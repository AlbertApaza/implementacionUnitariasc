-- --------------------------------------------------------
-- Host:                         161.132.38.250
-- Versión del servidor:         10.5.28-MariaDB-0+deb11u2 - Debian 11
-- SO del servidor:              debian-linux-gnu
-- HeidiSQL Versión:             12.10.0.7000
-- --------------------------------------------------------

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET NAMES utf8 */;
/*!50503 SET NAMES utf8mb4 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;


-- Volcando estructura de base de datos para proyectoconstruccion_apaza_cutipa
DROP DATABASE IF EXISTS `proyectoconstruccion_apaza_cutipa`;
CREATE DATABASE IF NOT EXISTS `proyectoconstruccion_apaza_cutipa` /*!40100 DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci */;
USE `proyectoconstruccion_apaza_cutipa`;

-- Volcando estructura para tabla proyectoconstruccion_apaza_cutipa.usuario
DROP TABLE IF EXISTS `usuario`;
CREATE TABLE IF NOT EXISTS `usuario` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `Nombre` varchar(100) NOT NULL,
  `Correo` varchar(100) NOT NULL,
  `Contrasena` varchar(100) DEFAULT NULL,
  `TipoUsuario` enum('Administrador','Empleado','Invitado') DEFAULT 'Invitado',
  `MetodoRegistro` enum('Credenciales','Google') NOT NULL,
  `Aprobado` tinyint(1) DEFAULT 0,
  `FechaRegistro` datetime DEFAULT current_timestamp(),
  PRIMARY KEY (`Id`),
  UNIQUE KEY `Correo` (`Correo`)
) ENGINE=InnoDB AUTO_INCREMENT=15 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- Volcando datos para la tabla proyectoconstruccion_apaza_cutipa.usuario: ~9 rows (aproximadamente)
REPLACE INTO `usuario` (`Id`, `Nombre`, `Correo`, `Contrasena`, `TipoUsuario`, `MetodoRegistro`, `Aprobado`, `FechaRegistro`) VALUES
	(1, 'admin', 'admin@gmail.com', '1234', 'Administrador', 'Credenciales', 1, '2025-05-21 13:31:47'),
	(2, 'dsdad', 'Daniel@gmail.com', '123456', 'Invitado', 'Credenciales', 1, '2025-05-22 01:10:30'),
	(3, 'ewe', 'ricardo@gmail.com', '123456', 'Invitado', 'Credenciales', 0, '2025-05-22 23:46:35'),
	(5, 'Ricardo Cutipa', 'rd372nhgp@gmail.com', NULL, 'Invitado', 'Google', 0, '2025-05-23 11:31:18'),
	(6, 'ejemplo', 'ejemplo@gmail.com', '123456', 'Invitado', 'Credenciales', 0, '2025-05-23 11:59:02'),
	(7, 'qwer', 'qwer@gmail.com', '123', 'Invitado', 'Credenciales', 0, '2025-05-23 12:06:48'),
	(12, 'sssssss', 'cutipadaniel11@gmail.com', '789', 'Administrador', 'Credenciales', 1, '2025-05-23 13:13:04'),
	(13, 'saaaaaaaa', 'sawa86523@gmail.com', '123', 'Invitado', 'Credenciales', 1, '2025-05-23 13:16:43'),
	(14, 'aaaaa', 'Aaniel@gmail.com', '123', 'Invitado', 'Credenciales', 1, '2025-05-23 13:19:19');

/*!40103 SET TIME_ZONE=IFNULL(@OLD_TIME_ZONE, 'system') */;
/*!40101 SET SQL_MODE=IFNULL(@OLD_SQL_MODE, '') */;
/*!40014 SET FOREIGN_KEY_CHECKS=IFNULL(@OLD_FOREIGN_KEY_CHECKS, 1) */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40111 SET SQL_NOTES=IFNULL(@OLD_SQL_NOTES, 1) */;
