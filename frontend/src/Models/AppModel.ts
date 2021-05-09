import { ModalActionTypes } from "./ModalModel";
import { serverActionTypes } from "./ServerModel";
import { SessionActionTypes } from "./SessionModel";
import { UserActionTypes } from "./UserModel";

// This will be a union type of all potential action types.
export type AppActions =
    SessionActionTypes |
    UserActionTypes |
    ModalActionTypes |
    serverActionTypes;