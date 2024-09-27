const express = require('express');
const { Client } = require('pg');
const app = express();

const client = new Client({
    connectionString: process.env.DB,
});

client.connect();

app.get('/', async (req, res) => {
    const result = await client.query('SELECT vote, COUNT(vote) AS count FROM votes GROUP BY vote');
    res.json(result.rows);
});

app.listen(80, () => {
    console.log('Result app listening on port 80');
});
