const express = require('express');
const { Pool } = require('pg');

const app = express();
const port = 3000;

// Configurar la conexión a PostgreSQL
const pool = new Pool({
    user: 'postgres',          // Reemplaza con tu usuario de PostgreSQL
    host: 'db',         // El host donde está corriendo PostgreSQL
    database: 'votesdb',       // Nombre de tu base de datos
    password: 'password',      // Reemplaza con tu contraseña de PostgreSQL
    port: 5432,                // Puerto por defecto de PostgreSQL
});

// Función para obtener los resultados de las votaciones
const getVotes = async () => {
    const query = 'SELECT "Animal", COUNT(*) AS votes FROM "Votes" GROUP BY "Animal" ORDER BY votes DESC';
    const { rows } = await pool.query(query);
    return rows;
};


// Endpoint para mostrar los resultados de las votaciones
app.get('/results', async (req, res) => {
    try {
        const votes = await getVotes();
        res.json(votes);
    } catch (error) {
        console.error('Error al obtener los resultados de las votaciones:', error);
        res.status(500).send('Error al obtener los resultados de las votaciones.');
    }
});

// Iniciar el servidor
app.listen(port, () => {
    console.log(`Servidor escuchando en http://localhost:${port}`);
});
