# -*- coding: utf-8 -*-
import cv2
import numpy as np
import tkinter as tk
from tkinter import filedialog, simpledialog, messagebox
from ultralytics import YOLO
import mss
import json
import datetime
import os
import requests
import time
from collections import deque
import threading
import mysql.connector

# ====== Configuración general ======
VPS_IP = "161.132.38.250"
API_PORT = 5001
UPLOAD_URL = f"http://{VPS_IP}:{API_PORT}/upload-fragment"
ALERT_URL = f"http://{VPS_IP}:{API_PORT}/alert"

CLASES_PARA_ALERTA_JSON = ['gun', 'knife', 'rifle', 'mask', 'helmet', 'person']
CLASES_CRITICAS_PARA_VIDEO = ['gun', 'knife', 'rifle', 'mask', 'helmet']

FRAME_RATE = 15.00
PRE_EVENT_BUFFER_SECONDS = 10
POST_DISAPPEARANCE_COOLDOWN_SECONDS = 3
JSON_ALERT_COOLDOWN_SECONDS = 13

last_json_alert_time_by_object = {}
json_alert_lock = threading.Lock()

PRE_EVENT_BUFFER_SIZE = int(FRAME_RATE * PRE_EVENT_BUFFER_SECONDS)

# ====== Database Config ======
DB_HOST = "161.132.38.250"
DB_USER = "root"
DB_PASSWORD = "patitochera123" # Consider using environment variables for passwords
DB_NAME = "proyectoconstruccion_apaza_cutipa"
DB_TABLE = "tbDeteccionEventos"
db_connection = None
db_cursor = None

# ====== State Variables for Dynamic Recording ======
is_actively_recording_event = False
last_critical_object_seen_time = 0
current_event_frames = deque()
pre_event_buffer = deque(maxlen=PRE_EVENT_BUFFER_SIZE)
video_event_trigger_info = None

# ====== Cargar modelo YOLO ======
model_path_user = "c:\\Users\\Kenyi\\Desktop\\visor\\best.pt"
model_path_default = "best.pt"
model_path_fallback = "yolov8n.pt"

if os.path.exists(model_path_user):
    model_path = model_path_user
elif os.path.exists(model_path_default):
    model_path = model_path_default
else:
    model_path = model_path_fallback
    print(f"Advertencia: No se encontró '{model_path_user}' ni '{model_path_default}'. Usando modelo de fallback: {model_path_fallback}")

try:
    model = YOLO(model_path)
    print(f"Modelo YOLO cargado: {model_path}")
    print(f"Clases detectables: {model.names}")
    CLASES_PARA_ALERTA_JSON = [cls for cls in CLASES_PARA_ALERTA_JSON if cls in model.names.values()]
    CLASES_CRITICAS_PARA_VIDEO = [cls for cls in CLASES_CRITICAS_PARA_VIDEO if cls in model.names.values()]
    print(f"Clases para Alerta JSON (filtradas): {CLASES_PARA_ALERTA_JSON}")
    print(f"Clases Críticas para Video (filtradas): {CLASES_CRITICAS_PARA_VIDEO}")
except Exception as e:
    print(f"Error al cargar el modelo YOLO: {e}")
    messagebox.showerror("Error Modelo", f"No se pudo cargar el modelo YOLO: {e}")
    exit()

def connect_db():
    global db_connection, db_cursor
    try:
        # Ensure any existing connection is closed before creating a new one
        if db_connection and db_connection.is_connected():
            db_cursor.close()
            db_connection.close()
            print("Conexión BD anterior cerrada.")
        db_connection = mysql.connector.connect(host=DB_HOST, user=DB_USER, password=DB_PASSWORD, database=DB_NAME, connect_timeout=10) # Increased timeout
        db_cursor = db_connection.cursor()
        print("Conexión BD establecida.")
        return True
    except mysql.connector.Error as err:
        print(f"Error conexión BD: {err}")
        db_connection = None
        db_cursor = None
        return False

def get_severidad(confianza):
    if confianza >= 0.8: return "Alta"
    elif confianza >= 0.5: return "Media"
    else: return "Baja"

def insert_event_db(event_data_dict_for_db):
    """Inserts event into DB and returns the ID of the inserted row or None on failure."""
    if not db_connection or not db_cursor or not db_connection.is_connected():
        if not connect_db():
            print("Fallo reconexión BD. No se guardará evento.")
            return None
    now = event_data_dict_for_db['timestamp_obj']
    # Note: IDdrive will be NULL initially. Make sure IDdrive column exists in your table.
    sql = f"INSERT INTO {DB_TABLE} (fecha, hora, tipo, subtipo, severidad, ubicacion, confianza, estado, IDdrive) VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s)"
    severidad_val = get_severidad(event_data_dict_for_db['confianza'])
    val = (now.strftime("%Y-%m-%d"), now.strftime("%H:%M:%S"), event_data_dict_for_db['objeto'], None, severidad_val,
           event_data_dict_for_db['fuente'], round(event_data_dict_for_db['confianza'], 2), "Grabado", None) # IDdrive is None initially
    try:
        db_cursor.execute(sql, val)
        db_connection.commit()
        event_id = db_cursor.lastrowid # This will be the value for idEvento
        print(f"Evento de VIDEO insertado en BD: ID {event_id}, {event_data_dict_for_db['objeto']}")
        return event_id
    except mysql.connector.Error as err:
        print(f"Error insertando evento BD: {err}")
        if db_connection and db_connection.is_connected():
            db_connection.rollback()
        return None
    except Exception as e:
        print(f"Excepción general insertando BD: {e}")
        if db_connection and db_connection.is_connected():
            db_connection.rollback()
        return None

def update_event_db_with_drive_id(db_event_id, drive_id_value):
    """Updates the event record with the Google Drive ID."""
    if not db_event_id or not drive_id_value:
        print("No se proporcionó ID de evento o ID de Drive para actualizar.")
        return

    if not db_connection or not db_cursor or not db_connection.is_connected():
        if not connect_db():
            print("Fallo reconexión BD. No se actualizará evento con ID de Drive.")
            return
    
    # Corrected primary key column name based on your table structure
    primary_key_column_name = "idEvento"
    sql = f"UPDATE {DB_TABLE} SET IDdrive = %s WHERE {primary_key_column_name} = %s"
    val = (drive_id_value, db_event_id)
    try:
        db_cursor.execute(sql, val)
        db_connection.commit()
        print(f"Evento ID {db_event_id} actualizado en BD con Drive ID: {drive_id_value}")
    except mysql.connector.Error as err:
        print(f"Error actualizando evento BD con Drive ID: {err}")
        if db_connection and db_connection.is_connected():
            db_connection.rollback()
    except Exception as e:
        print(f"Excepción general actualizando BD con Drive ID: {e}")
        if db_connection and db_connection.is_connected():
            db_connection.rollback()


def send_json_alert(alert_data_dict_json):
    global last_json_alert_time_by_object

    object_type = alert_data_dict_json.get("objeto", "desconocido").lower()
    current_time_sec = time.time()

    with json_alert_lock:
        if current_time_sec - last_json_alert_time_by_object.get(object_type, 0) < JSON_ALERT_COOLDOWN_SECONDS:
            return
        last_json_alert_time_by_object[object_type] = current_time_sec

    if ALERT_URL:
        alert_data_serializable = alert_data_dict_json.copy()
        if 'bbox' in alert_data_serializable and not isinstance(alert_data_serializable['bbox'], list):
            alert_data_serializable['bbox'] = [int(x) for x in alert_data_serializable['bbox']]
        if 'timestamp_obj' in alert_data_serializable and isinstance(alert_data_serializable['timestamp_obj'], datetime.datetime):
            alert_data_serializable['timestamp'] = alert_data_serializable.pop('timestamp_obj').isoformat()
        
        print(f"Intentando enviar ALERTA JSON: {alert_data_serializable.get('objeto','N/A')} @ {alert_data_serializable.get('confianza',0):.2f}")
        try:
            res = requests.post(ALERT_URL, json=alert_data_serializable, timeout=10)
            print(f"ALERTA JSON enviada para {object_type}. Respuesta: {res.status_code}")
            if not res.ok:
                print(f"Error servidor JSON para {object_type}: {res.text[:200]}")
        except requests.exceptions.RequestException as e:
            print(f"Error enviando ALERTA JSON para {object_type}: {e}")
        except TypeError as te:
            print(f"Error de tipo serializando ALERTA JSON para {object_type}: {te}")
    else:
        print(f"--- ALERT_URL no configurado para ALERTA JSON ({object_type}). ---")

# Tkinter y configuración de fuente 
root = tk.Tk(); root.withdraw()
opcion = simpledialog.askstring("Modo de Monitoreo", "Seleccione el modo de monitoreo:\n1 - Video desde archivo\n2 - Pantalla en vivo", parent=root)
cap = None; desc_fuente = ""; source_fps_actual = FRAME_RATE
if opcion == "1":
    ruta = filedialog.askopenfilename(filetypes=[("Videos", "*.mp4 *.avi *.mov *.mkv")], parent=root)
    if not ruta: exit()
    cap = cv2.VideoCapture(ruta)
    if not cap.isOpened(): messagebox.showerror("Error", "No se pudo abrir video."); exit()
    source_fps_actual = cap.get(cv2.CAP_PROP_FPS)
    if source_fps_actual == 0 or source_fps_actual > 200: # Some libraries return very high FPS for invalid files
        print(f"FPS del video ({source_fps_actual}) inválido o no legible, usando {FRAME_RATE:.2f} FPS por defecto.")
        source_fps_actual = FRAME_RATE
    desc_fuente = f"Video: {os.path.basename(ruta)}"
    print(f"Monitoreo video: {desc_fuente} (~{source_fps_actual:.2f} FPS)")
elif opcion == "2":
    desc_fuente = "Pantalla en vivo"; source_fps_actual = FRAME_RATE # For screen, we aim for this FPS
    print(f"Monitoreo pantalla (Objetivo: {source_fps_actual:.2f} FPS)")
else: messagebox.showerror("Error", "Opción inválida."); exit()

PRE_EVENT_BUFFER_SIZE = int(source_fps_actual * PRE_EVENT_BUFFER_SECONDS)
pre_event_buffer = deque(maxlen=PRE_EVENT_BUFFER_SIZE)
print(f"Buffer pre-evento: {PRE_EVENT_BUFFER_SIZE} frames ({PRE_EVENT_BUFFER_SECONDS}s @ {source_fps_actual:.2f} FPS)")
print(f"Cooldown post-desaparición (video): {POST_DISAPPEARANCE_COOLDOWN_SECONDS}s")
print(f"Cooldown alertas JSON (mismo objeto): {JSON_ALERT_COOLDOWN_SECONDS}s")
print("-" * 30)

def captura_pantalla():
    try:
        with mss.mss() as sct:
            monitor_idx = 1 # Default to primary monitor
            if len(sct.monitors) > 2 and sct.monitors[0]['left'] == 0 and sct.monitors[0]['top'] == 0 : 
                 monitor_idx = 2 
            elif len(sct.monitors) == 1: 
                 monitor_idx = 1
            
            if monitor_idx >= len(sct.monitors): 
                print(f"Advertencia: monitor_idx {monitor_idx} fuera de rango. Usando monitor 1.")
                monitor_idx = 1
            
            if len(sct.monitors) == 0:
                print("Error: No se detectaron monitores por mss.")
                return None

            monitor = sct.monitors[monitor_idx]
            img = np.array(sct.grab(monitor))
            return cv2.cvtColor(img, cv2.COLOR_BGRA2BGR)
    except Exception as e:
        print(f"Error en captura_pantalla: {e}")
        return None


def guardar_y_enviar_video_evento(frames_list, width, height, fps, obj_name, event_time_obj):
    """Saves video locally, uploads it, and returns Google Drive ID if successful."""
    ts_str = event_time_obj.strftime("%Y%m%d_%H%M%S")
    fname = f"EVENT_VIDEO_{obj_name}_{ts_str}.webm"
    
    temp_dir_base = os.path.join(os.getenv('TEMP', os.getenv('TMP', '/tmp')), 'ScreenMonitoringEvents') 
    os.makedirs(temp_dir_base, exist_ok=True)
    fpath = os.path.join(temp_dir_base, fname)
    
    print(f"--- Guardando VIDEO evento: {fname} ---")
    effective_fps = float(fps) if fps > 0 else float(FRAME_RATE) 

    fourcc = cv2.VideoWriter_fourcc(*'VP80') 
    out = cv2.VideoWriter(fpath, fourcc, effective_fps, (width, height))
    
    print(f"--- Escribiendo {len(frames_list)} frames ({len(frames_list)/effective_fps:.1f}s) a {effective_fps} FPS... ---")
    for frame_item in frames_list:
        out.write(frame_item)
    out.release()
    print(f"--- VIDEO evento guardado localmente: {fpath} ---")

    drive_id = None 
    if UPLOAD_URL:
        try:
            with open(fpath, 'rb') as f_vid:
                files = {'video': (fname, f_vid, 'video/webm')}
                print(f"--- Subiendo {fname} a {UPLOAD_URL}... ---")
                r = requests.post(UPLOAD_URL, files=files, timeout=60) 
                print(f"VIDEO evento enviado. Servidor respondió: {r.status_code}")
                if r.ok:
                    try:
                        response_json = r.json()
                        drive_id = response_json.get('driveId') 
                        if drive_id:
                            print(f"Google Drive ID recibido: {drive_id}")
                        else:
                            print(f"Respuesta OK del servidor, pero no se encontró 'driveId' en la respuesta JSON. Respuesta: {r.text[:200]}")
                    except ValueError: 
                        print(f"No se pudo decodificar JSON de la respuesta del servidor (status {r.status_code}): {r.text[:200]}")
                else:
                    print(f"Error del servidor al subir video: {r.status_code} - {r.text[:200]}")
        except requests.exceptions.RequestException as e_req:
            print(f"Error de red enviando VIDEO evento: {e_req}")
        except Exception as e:
            print(f"Error inesperado durante la subida de video: {e}")
    else:
        print("--- UPLOAD_URL no configurado. El video solo se guardó localmente. ---")
    return drive_id


def _process_video_and_update_db_task(frames_list, width, height, fps, obj_name, event_time_obj, db_event_id_to_update):
    """Task to be run in a thread: saves, uploads video, and updates DB with Drive ID."""
    print(f"Iniciando tarea de procesamiento de video para DB ID: {db_event_id_to_update}")
    drive_id = guardar_y_enviar_video_evento(frames_list, width, height, fps, obj_name, event_time_obj)
    if drive_id and db_event_id_to_update:
        print(f"Intentando actualizar DB ID {db_event_id_to_update} con Drive ID {drive_id}")
        update_event_db_with_drive_id(db_event_id_to_update, drive_id)
    elif not drive_id:
        print(f"No se obtuvo Drive ID para el evento DB ID {db_event_id_to_update}. No se actualiza.")
    elif not db_event_id_to_update: 
        print(f"Se obtuvo Drive ID {drive_id} pero no hay DB event ID para actualizar.")


def finalize_video_event_and_send(frame_w, frame_h, proc_fps, trigger_info_for_video_db):
    global is_actively_recording_event, current_event_frames, video_event_trigger_info
    
    if not current_event_frames or not trigger_info_for_video_db:
        is_actively_recording_event = False
        current_event_frames.clear()
        video_event_trigger_info = None
        return

    print(f"--- Finalizando VIDEO evento para '{trigger_info_for_video_db['objeto']}' ---")
    frames_to_process = list(current_event_frames) 

    db_event_id = insert_event_db(trigger_info_for_video_db.copy())

    if db_event_id:
        print(f"Evento DB ID {db_event_id} creado. Iniciando guardado/subida de video en segundo plano.")
        threading.Thread(target=_process_video_and_update_db_task,
                         args=(frames_to_process, frame_w, frame_h, proc_fps,
                               trigger_info_for_video_db['objeto'],
                               trigger_info_for_video_db['timestamp_obj'],
                               db_event_id)).start()
    else:
        print("Fallo al insertar evento inicial en BD. No se procesará video ni se subirá.")

    is_actively_recording_event = False
    current_event_frames.clear()
    video_event_trigger_info = None
    print("--- Solicitud de procesamiento de VIDEO evento enviada a segundo plano. Listo para nuevas detecciones. ---")


# ====== Bucle principal ======
if not connect_db():
    messagebox.showwarning("DB Error", "No se pudo conectar a la BD al inicio. Se reintentará en operaciones.")

frame_width_global, frame_height_global = 0, 0
try:
    last_frame_time = time.time()
    loop_interval = 1.0 / source_fps_actual 

    while True:
        current_time_loop_start = time.time()
        
        time_since_last_frame = current_time_loop_start - last_frame_time
        wait_duration = loop_interval - time_since_last_frame
        if wait_duration > 0:
            time.sleep(wait_duration)
        
        last_frame_time = time.time() 

        current_frame_raw = None
        if cap: 
            ret, current_frame_raw = cap.read()
            if not ret:
                print("Fin del video.")
                break
        else: 
            current_frame_raw = captura_pantalla()
            if current_frame_raw is None:
                time.sleep(0.1) 
                continue
        
        current_frame_processed = current_frame_raw.copy() 

        if frame_width_global == 0 and current_frame_processed is not None :
             frame_height_global, frame_width_global, _ = current_frame_processed.shape
        
        if current_frame_processed is None: continue 

        pre_event_buffer.append(current_frame_processed.copy()) 

        results = model(current_frame_processed, verbose=False, conf=0.4) 
        
        display_frame = current_frame_processed.copy() 
        detected_critical_object_this_frame = False
        most_confident_critical_for_video_info = None
        json_alerts_sent_this_frame_for_type = set()

        for result in results:
            for box in result.boxes:
                conf = float(box.conf[0])
                cls_id = int(box.cls[0])
                label = model.names.get(cls_id, f"id_{cls_id}").lower()
                x1, y1, x2, y2 = map(int, box.xyxy[0])
                
                current_timestamp = datetime.datetime.now()
                object_info = {
                    "tipo": "objeto_detectado", "objeto": label, "confianza": round(conf, 3),
                    "bbox": [x1, y1, x2, y2], "fuente": desc_fuente,
                    "timestamp_obj": current_timestamp 
                }

                if label in CLASES_PARA_ALERTA_JSON and conf > 0.45: 
                    if label not in json_alerts_sent_this_frame_for_type:
                        threading.Thread(target=send_json_alert, args=(object_info.copy(),)).start()
                        json_alerts_sent_this_frame_for_type.add(label)
                
                if label in CLASES_CRITICAS_PARA_VIDEO and conf > 0.5: 
                    detected_critical_object_this_frame = True
                    if not most_confident_critical_for_video_info or conf > most_confident_critical_for_video_info['confianza']:
                        most_confident_critical_for_video_info = object_info.copy()
                    
                    color = (0, 0, 255) 
                    text_disp = f"VIDEO-TRIG: {label.upper()} {conf:.2f}"
                elif label in CLASES_PARA_ALERTA_JSON and conf > 0.45: 
                    color = (0, 165, 255)
                    text_disp = f"ALERTA: {label.upper()} {conf:.2f}"
                elif conf > 0.3: 
                    color = (0, 255, 0)
                    text_disp = f"{label} {conf:.2f}"
                else: 
                    continue

                cv2.rectangle(display_frame, (x1, y1), (x2, y2), color, 2)
                cv2.putText(display_frame, text_disp, (x1, y1 - 10 if y1 > 20 else y2 + 15),
                            cv2.FONT_HERSHEY_SIMPLEX, 0.6, color, 2)

        current_loop_time = time.time() 

        if detected_critical_object_this_frame:
            last_critical_object_seen_time = current_loop_time 
            if not is_actively_recording_event and most_confident_critical_for_video_info:
                is_actively_recording_event = True
                current_event_frames.clear()
                current_event_frames.extend(list(pre_event_buffer)) 
                video_event_trigger_info = most_confident_critical_for_video_info.copy()
                print(f"--- ¡NUEVO EVENTO DE GRABACIÓN DE VIDEO ({video_event_trigger_info['objeto'].upper()})! Iniciando... Pre-buffer: {len(pre_event_buffer)} frames.---")
            
            if is_actively_recording_event and current_frame_processed is not None:
                current_event_frames.append(current_frame_processed) 
        
        elif is_actively_recording_event: 
            if current_frame_processed is not None:
                current_event_frames.append(current_frame_processed) 
            
            if current_loop_time - last_critical_object_seen_time > POST_DISAPPEARANCE_COOLDOWN_SECONDS:
                if frame_width_global > 0 and frame_height_global > 0 and video_event_trigger_info:
                    finalize_video_event_and_send(frame_width_global, frame_height_global, source_fps_actual, video_event_trigger_info)
                else:
                    print("Advertencia: Dimensiones de frame no válidas o no hay info de trigger. No se puede finalizar el evento de video.")
                    is_actively_recording_event = False
                    current_event_frames.clear()
                    video_event_trigger_info = None


        if current_frame_processed is not None: 
            cv2.imshow("Detección - Camera V6 (Press 'q' to quit)", display_frame)
        
        if cv2.waitKey(1) & 0xFF == ord('q'):
            print("Tecla 'q' presionada. Cerrando...")
            break

except KeyboardInterrupt:
    print("\nInterrupción por teclado. Cerrando...")
except Exception as e:
    print(f"Error principal en el bucle: {e}")
    import traceback
    traceback.print_exc()
finally:
    print("Iniciando proceso de finalización...")
    if is_actively_recording_event and current_event_frames and video_event_trigger_info:
        if frame_width_global > 0 and frame_height_global > 0:
            print("Cerrando durante un evento de video activo. Intentando finalizar y guardar...")
            finalize_video_event_and_send(frame_width_global, frame_height_global, source_fps_actual, video_event_trigger_info)
            print("Esperando a que las tareas en segundo plano (subida de video) tengan oportunidad de completarse...")
            time.sleep(10) 
        else:
            print("Cerrando durante evento de video, pero dimensiones de frame no válidas. No se puede guardar.")
    
    if cap:
        cap.release()
        print("Captura de video (archivo) liberada.")
    cv2.destroyAllWindows()
    print("Ventanas OpenCV destruidas.")

    if db_connection and db_connection.is_connected():
        try:
            db_cursor.close()
            db_connection.close()
            print("Conexión BD cerrada.")
        except mysql.connector.Error as err:
            print(f"Error al cerrar la conexión BD: {err}")
            
    print("Recursos liberados. Programa finalizado.")