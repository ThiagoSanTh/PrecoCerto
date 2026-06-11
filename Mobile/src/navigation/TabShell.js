import { createContext, useContext, useState, useCallback, useEffect } from 'react';
import { View } from 'react-native';
import { useLayoutProfile } from '../hooks/useLayoutProfile';
import CustomTabBar from './CustomTabBar';

const TabBarPropsContext = createContext(null);

export function useTabBarPropsSetter() {
  return useContext(TabBarPropsContext);
}

function CaptureTabBarProps({ props, onCapture }) {
  useEffect(() => {
    onCapture(props);
  }, [props, props.state.index, onCapture]);
  return null;
}

export default function TabShell({ children }) {
  const { useTopNav } = useLayoutProfile();
  const [tabBarProps, setTabBarProps] = useState(null);
  const capture = useCallback((props) => setTabBarProps(props), []);

  const renderTabBar = useCallback(
    (props) => {
      if (useTopNav) {
        return <CaptureTabBarProps props={props} onCapture={capture} />;
      }
      return <CustomTabBar {...props} />;
    },
    [useTopNav, capture]
  );

  return (
    <TabBarPropsContext.Provider value={setTabBarProps}>
      <View style={{ flex: 1 }}>
        {useTopNav && tabBarProps ? <CustomTabBar {...tabBarProps} /> : null}
        {children(renderTabBar)}
      </View>
    </TabBarPropsContext.Provider>
  );
}
