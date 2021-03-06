## Account.py
## INF 122 - W15
##
import ServerConnection

FAIL_KEYWORD = "FAIL"

class Account:
    
    def __init__(self):
        self._username = ""
        self._password = ""
        self._logged_in = False


    def login(self, connection, game_choice):
        login_string = self._username + ServerConnection.VALU_DELIM + self._password + ServerConnection.VALU_DELIM + game_choice
        print 'connection opened'
        player_id = connection.get_response()      #message 1 <-
        print 'received message 1'
        connection.send_request(login_string)      #message 2 ->
        print 'sent message 2'
        status_message = connection.get_response() #message 3 <-
        print 'received message 3'
        
        return (player_id, status_message)
            

    def logout(self, connection):
        connection.close_connection()


    def set_username(self, name):
        self._username = name


    def set_password(self, password):
        self._password = password


    def get_username(self):
        return self._username


    def get_password(self):
        return self._password


    def is_logged_in(self):
        return self._logged_in

    def set_logged_in(self, a_bool):
        self._logged_in = a_bool
