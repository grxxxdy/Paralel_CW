import socket
from enum import Enum

class MessageType(Enum):
    CONNECT = 0
    DISCONNECT = 1
    WELCOME = 2
    UNKNOWN = 3
    SEARCHFILES = 4

def send_message(sock, msg_type, payload):
    payload_bytes = payload.encode('utf-8')
    type_bytes = msg_type.value.to_bytes(4, byteorder='big')
    length_bytes = len(payload_bytes).to_bytes(4, byteorder='big')

    sock.sendall(type_bytes)
    sock.sendall(length_bytes)
    sock.sendall(payload_bytes)

def read_message(sock):
    type_bytes = sock.recv(4)
    length_bytes = sock.recv(4)

    msg_type_int = int.from_bytes(type_bytes, byteorder='big')
    length = int.from_bytes(length_bytes, byteorder='big')

    payload = b''
    while len(payload) < length:
        payload += sock.recv(length - len(payload))

    msg_type = MessageType(msg_type_int)

    return msg_type, payload.decode('utf-8')

class Client:
    def __init__(self):
        self.sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)

    def connect(self, ip, port):
        self.sock.connect((ip, port))
        send_message(self.sock, MessageType.CONNECT, "Hello from Python client")
        type = MessageType.UNKNOWN
        while type != MessageType.CONNECT:
            type, payload = read_message(self.sock)
            print("Server response of type", type, ": ", payload)

    def disconnect(self):
        send_message(self.sock, MessageType.DISCONNECT, "")
        self.sock.close()

    def search_files(self, word):
        send_message(self.sock, MessageType.SEARCHFILES, word)
        type, payload = read_message(self.sock)
        if len(payload) > 0:
            print("Files containing ", word, " :\n", payload)
        else:
            print("No Files containing ", word)


if __name__ == '__main__':
    client = Client()
    client.connect('192.168.0.175', 5000)

    while True:
        print('1 - Search Files')
        print('2 - Exit')
        readline = input()
        while readline != '1' and readline != '2':
            print('Please enter a correct option')
            readline = input()

        if readline == '1':
            print('Enter a keyword:')
            keyword = input()
            while keyword == '':
                print('Please enter a valid word')
                keyword = input()
            client.search_files(keyword)
        elif readline == '2':
            client.disconnect()
            exit(0)
