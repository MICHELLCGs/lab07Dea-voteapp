from flask import Flask, render_template, redirect, request
import requests
import redis

app = Flask(__name__)

# Configurar Redis
r = redis.Redis(host='redis', port=6379, db=0)

# API para obtener una imagen de perro aleatoria
DOG_API_URL = "https://dog.ceo/api/breeds/image/random"
VOTE_API_URL = "http://worker:8080/vote"

@app.route('/')
def index():
    # Verificar si la información del perro está en Redis
    if r.exists("dog_image") and r.exists("dog_name"):
        dog_image = r.get("dog_image").decode("utf-8")
        dog_name = r.get("dog_name").decode("utf-8")
    else:
        # Si no está en Redis, obtener de la API y almacenar en Redis
        response = requests.get(DOG_API_URL).json()
        dog_image = response["message"]
        dog_name = dog_image.split("/")[4]  # El nombre del perro está en la URL de la imagen
        
        r.set("dog_image", dog_image, ex=60)  # Guardar en Redis por 60 segundos
        r.set("dog_name", dog_name, ex=60)

    return render_template('index.html', dog_image=dog_image, dog_name=dog_name)


@app.route('/vote', methods=['POST'])
def vote():
    dog_name = request.form['dog_name']
    
    # Enviar el voto a la API externa usando query string
    response = requests.post(f"{VOTE_API_URL}?animal={dog_name}")
    
    if response.status_code == 200:
        # Limpiar Redis para obtener una nueva imagen y nombre
        r.delete("dog_image")
        r.delete("dog_name")
        return redirect('/')
    else:
        return "Error: No se pudo registrar el voto.", 500


@app.route('/change', methods=['GET'])
def change():
    # Limpiar Redis para obtener una nueva imagen y nombre
    r.delete("dog_image")
    r.delete("dog_name")
    return redirect('/')


if __name__ == '__main__':
    app.run(host='0.0.0.0', port=5000, debug=True)
