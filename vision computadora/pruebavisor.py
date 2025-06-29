import cv2
import numpy as np
import random
from ultralytics import YOLO
import os
import math

# --- Configuración ---
DEFAULT_MODEL_PATH = "best.pt"  # Modelo predeterminado si no se especifica uno
CANVAS_WIDTH = 1200         # Ancho de la imagen de salida
CANVAS_HEIGHT = 1600        # Alto de la imagen de salida (ajústalo si tienes muchas clases)
MARGIN = 50                 # Margen alrededor del área de texto
COLUMN_MARGIN = 30          # Espacio horizontal entre columnas
LINE_SPACING = 35           # Espacio vertical para cada clase (incluye swatch y texto)
SWATCH_SIZE = 25            # Tamaño del cuadrado de color (swatch)
TEXT_OFFSET_X = 10          # Espacio entre el swatch y el texto
FONT = cv2.FONT_HERSHEY_SIMPLEX
FONT_SCALE = 0.6
FONT_THICKNESS = 1
TITLE_FONT_SCALE = 0.9
TITLE_FONT_THICKNESS = 2
BACKGROUND_COLOR = (255, 255, 255) # Blanco
TEXT_COLOR = (0, 0, 0)          # Negro

# --- Función para generar colores visualmente distintos (simple) ---
# --- Función para generar colores visualmente distintos (corregida) ---
def generate_distinct_color(index, total_colors):
    """Genera colores tratando de espaciarlos en el espectro HSV (rango OpenCV uint8)."""
    # Usar HSV es mejor para obtener colores distintos que RGB aleatorio
    # OpenCV usa H: 0-179, S: 0-255, V: 0-255 para uint8
    hue = int(180 * index / total_colors)     # Rango 0-179 para OpenCV uint8
    saturation = random.randint(180, 255)     # Rango 180-255 (para colores vivos)
    value = random.randint(180, 255)          # Rango 180-255 (para colores brillantes)

    # Crear el array HSV con valores uint8 válidos
    hsv_color = np.uint8([[[hue, saturation, value]]])

    # Convertir de HSV a BGR (el formato de color estándar de OpenCV)
    bgr_color = cv2.cvtColor(hsv_color, cv2.COLOR_HSV2BGR)[0][0]

    # Devolver como tupla de enteros estándar (B, G, R)
    return (int(bgr_color[0]), int(bgr_color[1]), int(bgr_color[2]))

# --- Inicio del Script ---
if __name__ == "__main__":
    print("--- Visualizador de Clases del Modelo YOLO ---")

    # 1. Obtener la ruta del modelo del usuario
    model_path_input = input(f"Introduce la ruta a tu modelo YOLO (ej: best.pt)\n"
                           f"o presiona Enter para usar el predeterminado ({DEFAULT_MODEL_PATH}): ").strip()
    model_path = model_path_input if model_path_input else DEFAULT_MODEL_PATH

    # 2. Cargar el modelo YOLO
    model = None
    class_names = []
    class_indices = []
    try:
        print(f"\nCargando modelo desde: '{model_path}'...")
        if not os.path.exists(model_path) and model_path == DEFAULT_MODEL_PATH:
            print(f"Info: '{DEFAULT_MODEL_PATH}' no encontrado localmente. Ultralytics intentará descargarlo.")

        model = YOLO(model_path)
        print("Modelo cargado exitosamente.")

        # Verificar y obtener nombres de clase
        if hasattr(model, 'names') and model.names:
            if isinstance(model.names, dict):
                # El formato estándar es {index: 'name'}
                # Ordenar por índice para consistencia
                sorted_items = sorted(model.names.items())
                class_indices = [item[0] for item in sorted_items]
                class_names = [item[1] for item in sorted_items]
            elif isinstance(model.names, list):
                # Si es una lista, el índice es implícito
                class_indices = list(range(len(model.names)))
                class_names = model.names
            else:
                raise TypeError(f"Formato inesperado para model.names: {type(model.names)}")

            num_classes = len(class_names)
            if num_classes == 0:
                 raise ValueError("El modelo se cargó pero no contiene nombres de clase.")
            print(f"El modelo puede detectar {num_classes} clases.")

        else:
            raise AttributeError("El objeto del modelo cargado no tiene el atributo 'names' o está vacío.")

    except FileNotFoundError:
        print(f"Error: No se encontró el archivo del modelo en '{model_path}'.")
        print("Asegúrate de que la ruta sea correcta y el archivo exista.")
        exit()
    except Exception as e:
        print(f"Error al cargar el modelo YOLO desde '{model_path}':")
        print(e)
        print("\nAsegúrate de que Ultralytics esté instalado (`pip install ultralytics`)")
        print("y que el archivo del modelo sea válido.")
        exit()

    # 3. Generar colores para cada clase
    # class_colors = {name: generate_random_color() for name in class_names}
    class_colors = {name: generate_distinct_color(i, num_classes) for i, name in enumerate(class_names)}


    # 4. Crear el lienzo (canvas) para dibujar
    canvas = np.full((CANVAS_HEIGHT, CANVAS_WIDTH, 3), BACKGROUND_COLOR, dtype=np.uint8)

    # 5. Añadir un título a la imagen
    title_text = f"Clases Detectables por: {os.path.basename(model_path)} ({num_classes} total)"
    (title_w, title_h), _ = cv2.getTextSize(title_text, FONT, TITLE_FONT_SCALE, TITLE_FONT_THICKNESS)
    title_x = (CANVAS_WIDTH - title_w) // 2
    title_y = MARGIN # Posición Y del título
    cv2.putText(canvas, title_text, (title_x, title_y), FONT, TITLE_FONT_SCALE, TEXT_COLOR, TITLE_FONT_THICKNESS, cv2.LINE_AA)

    # 6. Calcular disposición en columnas
    drawable_height = CANVAS_HEIGHT - title_y - MARGIN * 2 # Alto útil para dibujar clases
    max_items_per_column = max(1, drawable_height // LINE_SPACING) # Clases por columna
    num_columns = math.ceil(num_classes / max_items_per_column)    # Número de columnas necesarias

    # Estimar ancho máximo del texto para calcular ancho de columna
    max_text_width = 0
    for idx, name in zip(class_indices, class_names):
        text = f"{idx}: {name}"
        (text_w, _), _ = cv2.getTextSize(text, FONT, FONT_SCALE, FONT_THICKNESS)
        max_text_width = max(max_text_width, text_w)

    # Ancho total de un item: swatch + espacio + texto
    item_width = SWATCH_SIZE + TEXT_OFFSET_X + max_text_width
    # Ancho total necesario (considerando márgenes entre columnas)
    total_content_width = (item_width * num_columns) + (COLUMN_MARGIN * (num_columns - 1))

    # Ajustar posición X inicial para centrar las columnas si caben
    start_draw_x = MARGIN
    if total_content_width < CANVAS_WIDTH - MARGIN * 2:
        start_draw_x = (CANVAS_WIDTH - total_content_width) // 2

    column_width = item_width + COLUMN_MARGIN # Ancho asignado a cada columna (incluye margen derecho)

    # 7. Dibujar cada clase en el lienzo
    current_col = 0
    current_row = 0
    base_y = title_y + MARGIN # Posición Y inicial bajo el título

    print("\nClases encontradas:")
    for i in range(num_classes):
        idx = class_indices[i]
        name = class_names[i]
        color = class_colors[name]

        # Calcular posición para el item actual
        draw_x = start_draw_x + current_col * column_width
        draw_y = base_y + current_row * LINE_SPACING

        # Dibujar el cuadro de color (swatch)
        pt1 = (draw_x, draw_y)
        pt2 = (draw_x + SWATCH_SIZE, draw_y + SWATCH_SIZE)
        cv2.rectangle(canvas, pt1, pt2, color, -1) # Relleno
        cv2.rectangle(canvas, pt1, pt2, (0,0,0), 1) # Borde negro delgado

        # Dibujar el texto (índice y nombre de la clase)
        text_x = draw_x + SWATCH_SIZE + TEXT_OFFSET_X
        # Centrar verticalmente el texto respecto al swatch
        (text_w, text_h), _ = cv2.getTextSize(f"{idx}: {name}", FONT, FONT_SCALE, FONT_THICKNESS)
        text_y = draw_y + SWATCH_SIZE // 2 + text_h // 2
        cv2.putText(canvas, f"{idx}: {name}", (text_x, text_y), FONT, FONT_SCALE, TEXT_COLOR, FONT_THICKNESS, cv2.LINE_AA)

        # Imprimir en consola también
        print(f"  - Índice {idx}: {name}")

        # Moverse a la siguiente posición (fila o columna)
        current_row += 1
        if current_row >= max_items_per_column:
            current_row = 0
            current_col += 1

    # 8. Guardar y mostrar la imagen resultante
    output_filename = f"yolo_classes_{os.path.basename(model_path).split('.')[0]}.png"
    try:
        cv2.imwrite(output_filename, canvas)
        print(f"\nImagen con las clases guardada como: '{output_filename}'")

        # Mostrar la imagen en una ventana
        window_title = f"Clases de {os.path.basename(model_path)} (Pulsa cualquier tecla para cerrar)"
        # Redimensionar si es demasiado grande para la pantalla (opcional)
        display_canvas = canvas.copy()
        screen_h, screen_w = 1080, 1920 # Asume una pantalla Full HD, ajústalo si es necesario
        if display_canvas.shape[0] > screen_h - 100 or display_canvas.shape[1] > screen_w - 100:
             scale = min((screen_h - 100) / display_canvas.shape[0], (screen_w - 100) / display_canvas.shape[1])
             display_canvas = cv2.resize(display_canvas, None, fx=scale, fy=scale, interpolation=cv2.INTER_AREA)

        cv2.imshow(window_title, display_canvas)
        print("\nMostrando la imagen. Pulsa cualquier tecla en la ventana de la imagen para salir.")
        cv2.waitKey(0) # Espera indefinidamente hasta que se presione una tecla
        cv2.destroyAllWindows() # Cierra la ventana

    except Exception as e:
        print(f"\nError al guardar o mostrar la imagen: {e}")

    print("\nScript finalizado.")