import { Dispatch } from "redux";
import { ILoginDTO, IRegisterDTO, ISessionErrors } from "../Models/SessionModel";
import { IUser } from "../Models/UserModel";
import { AppActions, AppState } from "../store";
import { utilLogin, utilLogout, utilRegister, decodeUser } from "../Util/SessionUtil";

// When a login or register form is submitted (show loading anim).
export const gettingSession = (): AppActions => ({
    type: "GETTING_SESSION"
} as const);

// When the response of the auth request comes back.
export const receiveSession = (user: IUser, token: string): AppActions => ({
    type: "RECEIVE_SESSION",
    token,
    user
} as const);

// When the user logs out.
export const removeSession = () => ({
    type: "REMOVE_SESSION"
} as const);

export const receiveRefreshedToken = (token: string) => ({
    type: "RECEIVE_REFRESHED_TOKEN",
    token
} as const);

export const receiveSessionErrors = (errors: ISessionErrors) => ({
    type: "RECEIVE_SESSION_ERRORS",
    errors
} as const);

export const clearSessionErrors = () => ({
    type: "CLEAR_SESSION_ERRORS"
} as const);

export const login = async (loginDTO: ILoginDTO) =>
    async (dispatch: Dispatch<AppActions>, getState: () => AppState) => {
        dispatch(gettingSession());
        try {
            const res = await utilLogin(loginDTO);
            const user = decodeUser(res.data.token);
            dispatch(receiveSession(user, res.data.token));
        } catch (error) {
            handleAuthErrors(error, dispatch);
        }
    };

export const register = async (registerDTO: IRegisterDTO) =>
    async (dispatch: Dispatch<AppActions>, getState: () => AppState) => {
        dispatch(gettingSession());
        try {
            const res = await utilRegister(registerDTO);
            const user = decodeUser(res.data.token);
            dispatch(receiveSession(user, res.data.token));
        } catch (error) {
            handleAuthErrors(error, dispatch);
        }
    };

export const logout = () =>
    (dispatch: Dispatch<AppActions>, getState: () => AppState) => {
        utilLogout();
        dispatch(removeSession());
    }

function handleAuthErrors(error: any, dispatch: Dispatch<AppActions>) {
    if (error.response?.data) {
        dispatch(receiveSessionErrors(
            error.response.data
        ));
    } else if (!error.response) {
        dispatch(receiveSessionErrors({ general: ["Unable to reach server, please check your connection."] }));
    }
}
