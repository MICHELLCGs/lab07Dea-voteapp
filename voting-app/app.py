from flask import Flask, request, render_template
import redis
import os

app = Flask(__name__)

redis_host = os.getenv('REDIS', 'redis')
redis_db = redis.StrictRedis(host=redis_host, port=6379, decode_responses=True)

@app.route('/', methods=['GET', 'POST'])
def index():
    if request.method == 'POST':
        vote = request.form['vote']
        redis_db.incr(vote)
    return render_template('index.html')

if __name__ == "__main__":
    app.run(host='0.0.0.0', port=80)
