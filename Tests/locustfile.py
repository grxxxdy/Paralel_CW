import socket
import random
import time
from locust import User, task, events
from enum import Enum

from locust.exception import StopUser


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

class TcpUser(User):

    def on_start(self):
        self.sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        start = time.time()
        try:
            self.sock.connect(("192.168.0.175", 5000))
            # receive WELCOME
            t, payload = read_message(self.sock)

            # send CONNECT
            send_message(self.sock, MessageType.CONNECT, "Hello from locust client")
            t, payload = read_message(self.sock)

            rt = (time.time() - start) * 1000
            events.request.fire(
                request_type="tcp",
                name="connect",
                response_time=rt,
                response_length=len(payload),
                exception=None
            )
        except Exception as e:
            rt = (time.time() - start) * 1000
            events.request.fire(
                request_type="tcp",
                name="connect",
                response_time=rt,
                response_length=0,
                exception=e,
            )

            raise StopUser()

    def on_stop(self):
        try:
            send_message(self.sock, MessageType.DISCONNECT, "")
            self.sock.close()
        except Exception:
            pass

    @task
    def search_files(self):
        random_word = random.choice(["apple", "king", "the", "love", "book", "cat"])
        start_time = time.time()

        send_message(self.sock, MessageType.SEARCHFILES, random_word)
        msg_type, payload = read_message(self.sock)

        try:
            send_message(self.sock, MessageType.SEARCHFILES, random_word)
            msg_type, payload = read_message(self.sock)
            rt = (time.time() - start_time) * 1000

            if msg_type == MessageType.SEARCHFILES:
                # success
                events.request.fire(
                    request_type="tcp",
                    name="search_files",
                    response_time=0,
                    response_length=len(payload),
                    exception=None
                )
            else:
                events.request.fire(
                    request_type="tcp",
                    name="search_files",
                    response_time=rt,
                    response_length=0,
                    exception=Exception("Unexpected msg type")
                )

        except Exception as ex:
            # failure
            rt = (time.time() - start_time) * 1000
            events.request.fire(
                request_type="tcp",
                name="search_files",
                response_time=rt,
                response_length=0,
                exception=ex,
            )

        raise StopUser()

    # @task
    # def spam_search(self):
    #     random_word = random.choice(["apple", "king", "the", "love", "book", "cat"])
    #     start_time = time.time()
    #
    #     try:
    #         send_message(self.sock, MessageType.SEARCHFILES, random_word)
    #         msg_type, payload = read_message(self.sock)
    #
    #         rt = (time.time() - start_time) * 1000
    #
    #         events.request.fire(
    #             request_type="tcp",
    #             name="spam_search",
    #             response_time=rt,
    #             response_length=len(payload),
    #             exception=None if msg_type == MessageType.SEARCHFILES else Exception("Wrong type")
    #         )
    #
    #     except Exception as ex:
    #         rt = (time.time() - start_time) * 1000
    #         events.request.fire(
    #             request_type="tcp",
    #             name="spam_search",
    #             response_time=rt,
    #             response_length=0,
    #             exception=ex
    #         )
    #         raise StopUser()